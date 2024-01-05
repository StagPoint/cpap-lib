using System;
using System.Collections.Generic;

using Avalonia;

using cpap_app.Controls;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Colors = QuestPDF.Helpers.Colors;

namespace cpap_app.Printing;

public class HistoryPrintDocument : IDocument
{
	public UserProfile            Profile    { get; set; }
	public List<HistoryGraphBase> Graphs     { get; set; }
	public DateTime               RangeStart { get; set; }
	public DateTime               RangeEnd   { get; set; }

	public HistoryPrintDocument( UserProfile user, List<HistoryGraphBase> graphs, DateTime rangeStart, DateTime rangeEnd )
	{
		Profile    = user;
		Graphs     = graphs;
		RangeStart = rangeStart;
		RangeEnd   = rangeEnd;
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
				.Text( $"Overview for {RangeStart:d} to {RangeEnd:d}" )
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
						     .Text( chart.GraphTitle.Text )
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

					    table
						    .Cell()
						    .ColumnSpan( 3 )
						    .AlignCenter()
						    .PaddingBottom( 4 )
						    .Row( legendContainer =>
						    {
							    legendContainer.Spacing( 2 );

							    legendContainer.AutoItem().Text( "Legend: " ).SemiBold();

							    legendContainer.AutoItem().Border( 0.5f ).Background( DataColors.GetLightThemeColor( 0 ).ToHex() ).PaddingHorizontal( 4 ).AlignMiddle().Text( "Maximum " ).FontSize( 6 ).FontColor( Colors.White );
							    legendContainer.AutoItem().Border( 0.5f ).Background( DataColors.GetLightThemeColor( 1 ).ToHex() ).PaddingHorizontal( 4 ).AlignMiddle().Text( "95th Percentile" ).FontSize( 6 ).FontColor( Colors.White );
							    legendContainer.AutoItem().Border( 0.5f ).Background( DataColors.GetLightThemeColor( 2 ).ToHex() ).PaddingHorizontal( 4 ).AlignMiddle().Text( "Median" ).FontSize( 6 ).FontColor( Colors.White );
							    legendContainer.AutoItem().Border( 0.5f ).Background( DataColors.GetLightThemeColor( 3 ).ToHex() ).PaddingHorizontal( 4 ).AlignMiddle().Text( "Minimum" ).FontSize( 6 ).FontColor( Colors.White );
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
