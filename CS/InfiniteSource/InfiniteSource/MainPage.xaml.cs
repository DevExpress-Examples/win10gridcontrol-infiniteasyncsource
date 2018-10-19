using DevExpress.Data;
using DevExpress.Data.Filtering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InfiniteSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage() {
            this.InitializeComponent();
            var source = new DevExpress.Data.InfiniteAsyncSource() {
                ElementType = typeof(IssueData)
            };
            source.FetchRows += (o, e) => {
                e.Result = FetchRowsAsync(e);
            };
            source.GetTotalSummaries += (o, e) => {
                e.Result = GetTotalSummariesAsync(e);
            };
            grid.ItemsSource = source;
            source.GetUniqueValues += (o, e) => {
                if (e.PropertyName == "Priority") {
                    var values = Enum.GetValues(typeof(Priority)).Cast<object>().ToArray();
                    e.Result = Task.FromResult(values);
                }
                else if (e.PropertyName != "Created") {
                    throw new InvalidOperationException();
                }
            };
        }
        static async Task<object[]> GetTotalSummariesAsync(GetSummariesAsyncEventArgs e)
        {
            IssueFilter filter = MakeIssueFilter(e.Filter);
            var summaryValues = await IssuesService.GetSummariesAsync(filter);
            return e.Summaries.Select(x => {
                if (x.SummaryType == SummaryType.Count)
                    return (object)summaryValues.Count;
                if (x.SummaryType == SummaryType.Max && x.PropertyName == "Created")
                    return summaryValues.LastCreated;
                throw new InvalidOperationException();
            }).ToArray();
        }
        static async Task<FetchRowsResult> FetchRowsAsync(FetchRowsAsyncEventArgs e)
        {
            IssueSortOrder sortOrder = GetIssueSortOrder(e);
            IssueFilter filter = MakeIssueFilter(e.Filter);
            const int pageSize = 30;
            var issues = await IssuesService.GetIssuesAsync(
                page: e.Skip / pageSize,
                pageSize: pageSize,
                sortOrder: sortOrder,
                filter: filter);
            return new FetchRowsResult(issues, hasMoreRows: issues.Length == pageSize);
        }
        static IssueFilter MakeIssueFilter(CriteriaOperator filter) {
            return filter.Match(
                binary: (propertyName, value, type) => {
                    if (propertyName == "Priority" && type == BinaryOperatorType.Equal)
                        return new IssueFilter(priority: (Priority)value);
                    if (propertyName == "Created") {
                        if (type == BinaryOperatorType.GreaterOrEqual)
                            return new IssueFilter(createdFrom: (DateTime)value);
                        if (type == BinaryOperatorType.Less)
                            return new IssueFilter(createdTo: (DateTime)value);
                    }
                    throw new InvalidOperationException();
                },
               and: filters => {
                   return new IssueFilter(
                       createdFrom: filters.Select(x => x.CreatedFrom).SingleOrDefault(x => x != null),
                       createdTo: filters.Select(x => x.CreatedTo).SingleOrDefault(x => x != null),
                       minVotes: filters.Select(x => x.MinVotes).SingleOrDefault(x => x != null),
                       priority: filters.Select(x => x.Priority).SingleOrDefault(x => x != null)
                   );
               },
               @null: default(IssueFilter)
           );
        }
        static IssueSortOrder GetIssueSortOrder(FetchRowsAsyncEventArgs e) {
            IssueSortOrder sortOrder = IssueSortOrder.Default;
            if (e.SortOrder.Length > 0) {
                var sort = e.SortOrder.Single();
                if (sort.PropertyName == "Created") {
                    if (sort.Direction != ListSortDirection.Descending)
                        throw new InvalidOperationException();
                    sortOrder = IssueSortOrder.CreatedDescending;
                }
                if (sort.PropertyName == "Votes") {
                    sortOrder = sort.Direction == ListSortDirection.Ascending
                        ? IssueSortOrder.VotesAscending
                        : IssueSortOrder.VotesDescending;
                }
            }
            return sortOrder;
        }
    }
}
