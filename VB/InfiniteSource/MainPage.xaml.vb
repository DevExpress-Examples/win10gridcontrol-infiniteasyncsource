Imports DevExpress.Data
Imports DevExpress.Data.Filtering
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices.WindowsRuntime
Imports System.Threading.Tasks
Imports Windows.Foundation
Imports Windows.Foundation.Collections
Imports Windows.UI.Xaml
Imports Windows.UI.Xaml.Controls
Imports Windows.UI.Xaml.Controls.Primitives
Imports Windows.UI.Xaml.Data
Imports Windows.UI.Xaml.Input
Imports Windows.UI.Xaml.Media
Imports Windows.UI.Xaml.Navigation

' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Namespace InfiniteSource
    ''' <summary>
    ''' An empty page that can be used on its own or navigated to within a Frame.
    ''' </summary>
    Public NotInheritable Partial Class MainPage
        Inherits Page

        Public Sub New()
            Me.InitializeComponent()
            Dim source = New DevExpress.Data.InfiniteAsyncSource() With {.ElementType = GetType(IssueData)}
            AddHandler source.FetchRows, Sub(o, e)
                e.Result = FetchRowsAsync(e)
            End Sub
            AddHandler source.GetTotalSummaries, Sub(o, e)
                e.Result = GetTotalSummariesAsync(e)
            End Sub
            grid.ItemsSource = source
            AddHandler source.GetUniqueValues, Sub(o, e)
                If e.PropertyName = "Priority" Then
                    Dim values = System.Enum.GetValues(GetType(Priority)).Cast(Of Object)().ToArray()
                    e.Result = Task.FromResult(values)
                ElseIf e.PropertyName <> "Created" Then
                    Throw New InvalidOperationException()
                End If
            End Sub
        End Sub
        Private Shared Async Function GetTotalSummariesAsync(ByVal e As GetSummariesAsyncEventArgs) As Task(Of Object())
            Dim filter As IssueFilter = MakeIssueFilter(e.Filter)
            Dim summaryValues = Await IssuesService.GetSummariesAsync(filter)
            Return e.Summaries.Select(Function(x)
                If x.SummaryType = SummaryType.Count Then
                    Return DirectCast(summaryValues.Count, Object)
                End If
                If x.SummaryType = SummaryType.Max AndAlso x.PropertyName = "Created" Then
                    Return summaryValues.LastCreated
                End If
                Throw New InvalidOperationException()
            End Function).ToArray()
        End Function
        Private Shared Async Function FetchRowsAsync(ByVal e As FetchRowsAsyncEventArgs) As Task(Of FetchRowsResult)
            Dim sortOrder As IssueSortOrder = GetIssueSortOrder(e)
            Dim filter As IssueFilter = MakeIssueFilter(e.Filter)
            Const pageSize As Integer = 30
            Dim issues = Await IssuesService.GetIssuesAsync(page:= e.Skip / pageSize, pageSize:= pageSize, sortOrder:= sortOrder, filter:= filter)
            Return New FetchRowsResult(issues, hasMoreRows:= issues.Length = pageSize)
        End Function
        Private Shared Function MakeIssueFilter(ByVal filter As CriteriaOperator) As IssueFilter
            Return filter.Match(binary:= Function(propertyName, value, type)
                If propertyName = "Priority" AndAlso type = BinaryOperatorType.Equal Then
                    Return New IssueFilter(priority:= CType(value, Priority))
                End If
                If propertyName = "Created" Then
                    If type = BinaryOperatorType.GreaterOrEqual Then
                        Return New IssueFilter(createdFrom:= CDate(value))
                    End If
                    If type = BinaryOperatorType.Less Then
                        Return New IssueFilter(createdTo:= CDate(value))
                    End If
                End If
                Throw New InvalidOperationException()
            End Function, [and]:= Function(filters)
                Return New IssueFilter(createdFrom:= filters.Select(Function(x) x.CreatedFrom).SingleOrDefault(Function(x) x IsNot Nothing), createdTo:= filters.Select(Function(x) x.CreatedTo).SingleOrDefault(Function(x) x IsNot Nothing), minVotes:= filters.Select(Function(x) x.MinVotes).SingleOrDefault(Function(x) x IsNot Nothing), priority:= filters.Select(Function(x) x.Priority).SingleOrDefault(Function(x) x IsNot Nothing))
            End Function, null:= Nothing)
        End Function
        Private Shared Function GetIssueSortOrder(ByVal e As FetchRowsAsyncEventArgs) As IssueSortOrder
            Dim sortOrder As IssueSortOrder = IssueSortOrder.Default
            If e.SortOrder.Length > 0 Then
                Dim sort = e.SortOrder.Single()
                If sort.PropertyName = "Created" Then
                    If sort.Direction <> ListSortDirection.Descending Then
                        Throw New InvalidOperationException()
                    End If
                    sortOrder = IssueSortOrder.CreatedDescending
                End If
                If sort.PropertyName = "Votes" Then
                    sortOrder = If(sort.Direction = ListSortDirection.Ascending, IssueSortOrder.VotesAscending, IssueSortOrder.VotesDescending)
                End If
            End If
            Return sortOrder
        End Function
    End Class
End Namespace
