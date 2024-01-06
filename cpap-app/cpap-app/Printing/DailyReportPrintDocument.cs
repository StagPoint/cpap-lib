using System;
using System.Collections.Generic;
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

using SQLite;

using Colors = QuestPDF.Helpers.Colors;

namespace cpap_app.Printing;

public class DailyReportPrintDocument : IDocument
{
	public UserProfile       Profile { get; set; }
	public List<SignalChart> Graphs  { get; set; }
	public DailyReport       Day     { get; set; }

	public DailyReportPrintDocument( UserProfile user, List<SignalChart> charts, DailyReport day )
	{
		Profile = user;
		Graphs  = charts;
		Day     = day;
	}
	
	public void Compose( IDocumentContainer document )
	{
		var pageWidth = PageSizes.Letter.Width;

		var graphSize = new PixelSize( 1280, 180 );

		document.Page( page =>
		{
			page.Size( PageSizes.Letter.Landscape() );
			page.Margin( 8 );
			page.PageColor( Colors.White );
			page.DefaultTextStyle( x => x.FontSize( 6 ).FontFamily( Fonts.SegoeUI ) );

			page
				.Header()
				.AlignCenter()
				.PaddingBottom( 8 )
				.Text( $"Detail View for {Day.ReportDate.Date:D}" )
				.FontSize( 12 );

			page.Content().Column( container =>
			{
				container.Item().Table( outerTable =>
				{
					outerTable.ColumnsDefinition( columns =>
					{
						columns.ConstantColumn( 200 );
						columns.RelativeColumn();
					} );

					outerTable.Cell().Border( 0.5f ).BorderColor( Colors.Grey.Lighten2 ).PaddingHorizontal( 4 ).Column( detailsColumn =>
					{
						ComposeGeneralInfo( detailsColumn );
						ComposeEventSummary( detailsColumn );
						ComposeStatistics( detailsColumn );
					} );

					outerTable.Cell().Column( graphColumn =>
					{
						foreach( var chart in Graphs )
						{
							graphColumn.Item().Border( 0.5f ).BorderColor( Colors.Grey.Lighten2 ).Table( table =>
							{
								table.ColumnsDefinition( columns =>
								{
									columns.ConstantColumn( 18 );
									columns.RelativeColumn();
								} );

								table.Cell()
								     .Element( GraphTitle )
								     .Text( chart.ChartLabel.Text )
								     .FontSize( 8 )
								     .SemiBold()
								     .FontColor( Colors.Grey.Darken3 );

								using var imageStream = chart.RenderGraphToBitmap( graphSize );
								table.Cell().PaddingRight( 8 ).AlignMiddle().Image( imageStream ).FitWidth();
							} );
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

	private void ComposeStatistics( ColumnDescriptor column )
	{
		column
			.Item()
			.PaddingTop( 12 )
			.PaddingBottom( 2 )
			.Background( Colors.Grey.Lighten2 )
			.PaddingHorizontal( 2 )
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

			foreach( var stat in Day.Statistics )
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
			.Background( Colors.Grey.Lighten2 )
			.PaddingHorizontal( 2 )
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
				indexTable.Cell().PaddingRight( 8 ).Text( index.Name ).SemiBold();
				indexTable.Cell().Text( $"{index.IndexValue:F2}" ).SemiBold();
				indexTable.Cell().Text( index.IndexValue > 0 ? $"{index.TotalTime:hh\\:mm\\:ss}" : "" ).SemiBold();
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
				table.Cell().PaddingLeft( 2 ).Text( $"{summary.TotalTime:hh\\:mm\\:ss}" );
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
			text.DefaultTextStyle( x => x.FontSize( 7 ).SemiBold() );
			
			text.Line( $"Device: {Day.MachineInfo.ProductName}" );
			text.Line( $"Model: {Day.MachineInfo.ModelNumber}" );
			text.Line( "" ).FontSize( 4 );
			// text.Line( $"Date: {Day.ReportDate.Date:D}" );
			text.Line( $"Start Time: {Day.RecordingStartTime:g}" );
			text.Line( $"End Time: {Day.RecordingEndTime:g}" );
			text.Line( $"Total Sleep Time: {Day.TotalSleepTime:g}" );
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

	static IContainer PrimaryColumnHeader( IContainer container )
	{
		return container
		       .Border( 0.5f )
		       .Background( Colors.Grey.Lighten2 )
		       .PaddingLeft( 2, Unit.Point )
		       .PaddingRight( 2, Unit.Point )
		       .AlignTop();
	}
}
