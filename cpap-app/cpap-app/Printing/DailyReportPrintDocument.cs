using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;

using cpap_app.Controls;
using cpap_app.Converters;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Colors = QuestPDF.Helpers.Colors;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace cpap_app.Printing;

public class DailyReportPrintDocument : IDocument
{
	public UserProfile       Profile      { get; set; }
	public EventGraph        EventGraph   { get; set; }
	public List<SignalChart> SignalCharts { get; set; }
	public DailyReport       Day          { get; set; }
	public DateRange         Selection    { get; set; }

	public DailyReportPrintDocument( UserProfile user, EventGraph eventGraph, List<SignalChart> charts, DailyReport day, DateRange selection )
	{
		Profile      = user;
		EventGraph   = eventGraph;
		SignalCharts = charts;
		Day          = day;
		Selection    = selection;
	}
	
	public void Compose( IDocumentContainer document )
	{
		var graphSize = new PixelSize( 1280, 200 );

		document.Page( page =>
		{
			page.Size( PageSizes.Letter.Landscape() );
			page.Margin( 8 );
			page.PageColor( Colors.White );
			page.DefaultTextStyle( x => x.FontSize( 6 ).FontFamily( Fonts.SegoeUI ) );

			page
				.Header()
				.AlignCenter()
				.Text( headerText =>
				{
					headerText.Line( $"Detail View for {Day.ReportDate.Date:D}" ).FontSize( 10 );
					
					//if( Selection.Duration.TotalMinutes < Day.TotalTimeSpan.TotalMinutes )
					{
						var duration      = FormattedTimespanConverter.FormatTimeSpan( Selection.Duration, TimespanFormatType.Abbreviated, false );
						var selectionText = $"Showing selection of {duration} from {Selection.Start:g} to {Selection.End:g}";
						
						headerText.Line( selectionText ).FontSize( 6 );
					}
				} );

			page.Content().Column( container =>
			{
				container.Item().Table( outerTable =>
				{
					outerTable.ColumnsDefinition( columns =>
					{
						columns.ConstantColumn( 180 );
						columns.RelativeColumn();
					} );
					
					outerTable.Cell().Border( 0.5f ).BorderColor( Colors.Grey.Lighten2 ).PaddingHorizontal( 2 ).Column( detailsColumn =>
					{
						ComposeGeneralInfo( detailsColumn );
						ComposeEventSummary( detailsColumn );
						ComposeStatistics( detailsColumn );
						ComposeDeviceSettings( detailsColumn );
					} );

					outerTable.Cell().Column( graphColumn =>
					{
						if( EventGraph is { IsVisible: true } )
						{
							ComposeSignalGraph( graphColumn, "Events", EventGraph.RenderGraphToBitmap( graphSize ) );
						}

						foreach( var chart in SignalCharts )
						{
							using var imageStream = chart.RenderGraphToBitmap( graphSize );
							var       graphLabel  = chart.ChartLabel.Text;
							
							ComposeSignalGraph( graphColumn, graphLabel, imageStream );
						}
					} );
				} );

				page.Footer()
				    .ExtendHorizontal()
				    .AlignCenter()
				    .Table( table =>
				    {
					    table.ColumnsDefinition( columns =>
					    {
						    columns.RelativeColumn();
						    columns.RelativeColumn( 3 );
						    columns.RelativeColumn();
					    } );

					    table.Cell()
					         .AlignLeft()
					         .Text( $"User Profile: {Profile.UserName}" );

					    table.Cell()
					         .AlignCenter()
					         .Text( $"Printed on {DateTime.Today:D} at {DateTime.Now:t}" );

					    table.Cell()
					         .AlignRight()
					         .Text( x =>
					         {
						         x.Span( "Page " );
						         x.CurrentPageNumber();
						         x.Span( " of " );
						         x.TotalPages();
					         } );
				    } );
			} );
		} );
	}
	
	private static void ComposeSignalGraph( ColumnDescriptor container, string? label, Stream image )
	{
		container.Item().Border( 0.5f ).BorderColor( Colors.Grey.Lighten2 ).Table( table =>
		{
			table.ColumnsDefinition( columns =>
			{
				columns.ConstantColumn( 18 );
				columns.RelativeColumn();
			} );

			table.Cell()
			     .Element( GraphTitle )
			     .Text( label )
			     .FontSize( 8 )
			     .SemiBold()
			     .FontColor( Colors.Grey.Darken3 );

			table.Cell().PaddingRight( 8 ).AlignMiddle().Image( image ).FitWidth();
		} );
		
		return;

		static IContainer GraphTitle( IContainer container )
		{
			return container
			       .PaddingBottom( 8 )
			       .RotateLeft()
			       .AlignCenter()
			       .AlignBottom();
		}
	}

	private void ComposeDeviceSettings( ColumnDescriptor column )
	{
		var viewModel = MachineSettingsViewModel.Generate( Day );

		column
			.Item()
			.StopPaging()
			.PaddingTop( 12 )
			.PaddingBottom( 2 )
			.Element( PrimaryColumnHeader )
			.Text( "Device Settings" )
			.FontSize( 8 )
			.SemiBold();
		
		column.Item().StopPaging().Table( indexTable =>
		{
			indexTable.ColumnsDefinition( columns =>
			{
				columns.RelativeColumn();
				columns.RelativeColumn();
			});
			
			foreach( var setting in viewModel.Settings )
			{
				indexTable.Cell().StopPaging().PaddingRight( 8 ).PaddingLeft( 2 ).Text( setting.Name );
				indexTable.Cell().StopPaging().PaddingLeft( 2 ).Text( $"{setting.Value} {setting.Units}" );
			}
		});
	}
	
	private void ComposeStatistics( ColumnDescriptor column )
	{
		var viewModel = new DailyStatisticsViewModel( Day );
		
		column
			.Item()
			.PaddingTop( 12 )
			.PaddingBottom( 2 )
			.Element( PrimaryColumnHeader )
			.Text( "Statistics" )
			.FontSize( 8 )
			.SemiBold();
		
		column.Item().Table( table =>
		{
			table.ColumnsDefinition( columns =>
			{
				columns.RelativeColumn( 2 );
				columns.RelativeColumn();
				columns.RelativeColumn();
				columns.RelativeColumn();
			});

			table.Cell().Element( PrimaryColumnHeader ).Text( "Name" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "Min" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "Median" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "95%" ).SemiBold();

			foreach( var stat in viewModel.Statistics )
			{
				table.Cell().PaddingLeft( 2 ).Text( stat.SignalName );
				table.Cell().PaddingLeft( 2 ).Text( $"{stat.Minimum:F2}" );
				table.Cell().PaddingLeft( 2 ).Text( $"{stat.Median:F2}" );
				table.Cell().PaddingLeft( 2 ).Text( $"{stat.Percentile95:F2}" );
			}
		});
	}
	
	private void ComposeEventSummary( ColumnDescriptor column )
	{
		var viewModel = GenerateEventSummaryViewModel();

		column
			.Item()
			.PaddingBottom( 2 )
			.Element( PrimaryColumnHeader )
			.Text( "Reported Events" )
			.FontSize( 8 )
			.SemiBold();

		column.Item().Table( indexTable =>
		{
			indexTable.ColumnsDefinition( columns =>
			{
				columns.RelativeColumn( 3 );
				columns.RelativeColumn();
				columns.RelativeColumn();
			});
			
			foreach( var index in viewModel.Indexes )
			{
				indexTable.Cell().PaddingLeft( 2 ).PaddingRight( 8 ).Text( index.Name ).SemiBold();
				indexTable.Cell().PaddingLeft( 2 ).Text( $"{index.IndexValue:F2}" ).SemiBold();
				indexTable.Cell().PaddingLeft( 2 ).Text( index.IndexValue > 0 ? $"{index.TotalTime:hh\\:mm\\:ss}" : "" ).SemiBold();
			}
		});

		column.Item().Text( string.Empty ).FontSize( 2 );
		
		column.Item().Table( table =>
		{
			table.ColumnsDefinition( columns =>
			{
				columns.RelativeColumn( 2 );
				columns.RelativeColumn();
				columns.RelativeColumn();
				columns.RelativeColumn();
			});

			table.Cell().Element( PrimaryColumnHeader ).Text( "Event" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "#/Hour" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "Total" ).SemiBold();
			table.Cell().Element( PrimaryColumnHeader ).Text( "Time" ).SemiBold();

			foreach( var summary in viewModel.Items )
			{
				table.Cell().PaddingLeft( 2 ).Text( NiceNames.Format( summary.Type.ToString() ) );
				table.Cell().PaddingLeft( 2 ).Text( $"{summary.IndexValue:F2}" );
				table.Cell().PaddingLeft( 2 ).Text( $"{summary.TotalCount:N0}" );
				table.Cell().PaddingLeft( 2 ).Text( summary.TotalTime.TotalSeconds > 0 ? $"{summary.TotalTime:hh\\:mm\\:ss}" : "" );
			}
		});
	}
	
	private EventSummaryViewModel GenerateEventSummaryViewModel()
	{
		var viewModel = new EventSummaryViewModel( Day );

		if( Day.HasDetailData )
		{
			viewModel.Indexes.Add( new EventGroupSummary( "Apnea/Hypopnea Index (AHI)", EventTypes.Apneas, Day.TotalSleepTime, Day.Events ) );

			if( Day.Events.Any( x => EventTypes.RespiratoryDisturbancesOnly.Contains( x.Type ) ) )
			{
				viewModel.Indexes.Add( new EventGroupSummary( "Respiratory Disturbance (RDI)", EventTypes.RespiratoryDisturbance, Day.TotalSleepTime, Day.Events ) );
			}
		}
		else
		{
			// Only summary information is available, so create a simplified GroupSummary instead 
			viewModel.Indexes.Add( new EventGroupSummary( "Apnea/Hypopnea Index (AHI)", Day.TotalSleepTime, Day.EventSummary.AHI ) );
		}

		return viewModel;
	}

	private void ComposeGeneralInfo( ColumnDescriptor column )
	{
		column.Item().Text( text =>
		{
			text.DefaultTextStyle( x => x.FontSize( 6 ).SemiBold() );

			text.Line( $"Device: {Day.MachineInfo.ProductName}" );
			text.Line( $"Model: {Day.MachineInfo.ModelNumber}" );
			text.Line( "" ).FontSize( 4 );
			// text.Line( $"Date: {Day.ReportDate.Date:D}" );
			text.Line( $"Start Time: {Day.RecordingStartTime:g}" );
			text.Line( $"End Time: {Day.RecordingEndTime:g}" );
			text.Line( $"Total Sleep Time: {Day.TotalSleepTime:g}" );

			/*
			if( SelectionLength != null )
			{
				var window = SelectionLength.Value.TrimSeconds();

				if( window.TotalMinutes < Day.TotalSleepTime.TotalMinutes )
				{
					text.Line( $"Selected Time: {FormattedTimespanConverter.FormatTimeSpan( window, TimespanFormatType.Short, true )}" );
				}
			}
			*/
		} );
	}
	
	static IContainer SectionHeader( IContainer container )
	{
		return container
		       .Border( 0.5f )
		       .Background( Colors.Grey.Lighten3 )
		       .PaddingLeft( 2, Unit.Point )
		       .PaddingRight( 2, Unit.Point )
		       .AlignTop();
	}

	private static IContainer PrimaryColumnHeader( IContainer container )
	{
		return container
		       .Border( 0.5f )
		       .BorderColor( Colors.Grey.Lighten1 )
		       .Background( Colors.Grey.Lighten2 )
		       .PaddingLeft( 2, Unit.Point )
		       .PaddingRight( 2, Unit.Point )
		       .AlignTop();
	}
}
