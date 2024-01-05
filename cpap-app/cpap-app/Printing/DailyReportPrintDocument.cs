using System;
using System.Collections.Generic;

using Avalonia;

using cpap_app.Controls;
using cpap_app.Helpers;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

		var graphSize = new PixelSize( (int)(pageWidth * 2), 200 );

		document.Page( page =>
		{
			page.Size( PageSizes.Letter );
			page.Margin( 8 );
			page.PageColor( Colors.White );
			page.DefaultTextStyle( x => x.FontSize( 8 ).FontFamily( Fonts.SegoeUI ) );

			page
				.Header()
				.AlignCenter()
				.PaddingBottom( 8 )
				.Text( $"Daily Report for {Day.ReportDate.Date:D}" )
				.FontSize( 12 );

			page.Content().Column( container =>
			{
				foreach( var chart in Graphs )
				{
					container.Item().BorderHorizontal( 0.5f ).BorderColor( Colors.Grey.Lighten2 ).Table( table =>
					{
						table.ColumnsDefinition( columns =>
						{
							columns.ConstantColumn( 18 );
							columns.RelativeColumn();
						} );

						table.Cell()
						     .Element( GraphTitle )
						     .Text( chart.ChartLabel.Text )
						     .SemiBold()
						     .FontColor( Colors.Grey.Darken3 );

						using var imageStream = chart.RenderGraphToBitmap( graphSize );
						table.Cell().PaddingRight( 8 ).AlignMiddle().Image( imageStream ).FitWidth();
					} );
				}

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
}
