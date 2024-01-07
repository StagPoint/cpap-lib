using System;
using System.Collections.Generic;
using System.Diagnostics;

using Avalonia.Media;

using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Colors = QuestPDF.Helpers.Colors;
using FontWeight = Avalonia.Media.FontWeight;

namespace cpap_app.Printing;

public class StatisticsPrintDocument : IDocument
{
	public UserProfile                Profile   { get; set; }
	public TherapyStatisticsViewModel ViewModel { get; set; }

	public StatisticsPrintDocument( UserProfile user, TherapyStatisticsViewModel viewModel )
	{
		Profile   = user;
		ViewModel = viewModel;
	}
	
	public void Compose( IDocumentContainer document )
	{
		document.Page( page =>
		{
			var columnHeaders = CalculateColumnWidths( Fonts.SegoeUI, 8 );

			// Switch to Landscape mode if necessary 
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if( columnHeaders.TotalWidth + 150 >= PageSizes.Letter.Width )
			{
				page.Size( PageSizes.Letter.Landscape() );
			}
			else
			{
				page.Size( PageSizes.Letter );
			}
			
			page.PageColor( Colors.White );
			page.DefaultTextStyle( x => x.FontSize( 8 ).FontFamily( Fonts.SegoeUI ) );

			page.Content().Column( column =>
			{
				foreach( var section in ViewModel.Sections )
				{
					ComposeSection( column, section, columnHeaders );
				}
			});

			page.Footer()
			    .AlignCenter()
			    .Padding( 8 )
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
	}
	
	private ColumnHeaderList CalculateColumnWidths( string fontFamily, float fontSize )
	{
		var typeFace = new Typeface( fontFamily, FontStyle.Normal, FontWeight.SemiBold );
		
		var headers = ViewModel.Headers;
		var widths  = new List<uint>( headers.Count + 1 ) { 50 };
		var labels  = new List<string>( headers.Count + 1 ) { "Details" };
		
		for( int i = 0; i < headers.Count; i++ )
		{
			var label = $"{headers[ i ].Label}\n{headers[i].DateLabel}";
			
			labels.Add( label );
			widths.Add( (uint)Math.Ceiling( PdfHelper.MeasureText( typeFace, label, fontSize ) ) );
		}

		foreach( var section in ViewModel.Sections )
		{
			foreach( var group in section.Groups )
			{
				foreach( var item in group.Items )
				{
					var values = item.Values;
					Debug.Assert( values.Count == headers.Count, "Item count mismatch" );

					var itemLabelWidth = (uint)Math.Ceiling( PdfHelper.MeasureText( typeFace, item.Label, fontSize ) );
					widths[ 0 ] = Math.Max( widths[ 0 ], itemLabelWidth );

					for( int i = 0; i < headers.Count; i++ )
					{
						var valueWidth = (uint)Math.Ceiling( PdfHelper.MeasureText( typeFace, values[ i ], fontSize ) );
						widths[ i + 1 ] = Math.Max( widths[ i + 1 ], valueWidth );
					}
				}
			}
		}

		return new ColumnHeaderList
		{
			HeaderLabels = labels,
			HeaderWidths = widths
		};
	}

	private static void ComposeSection( ColumnDescriptor container, TherapyStatisticsSectionViewModel section, ColumnHeaderList columnHeaders )
	{
		container.Item().AlignCenter().ShrinkHorizontal().ShowEntire().PaddingTop( 8 ).Column( column =>
		{
			column.Spacing( 8 );
  
			column
				.Item()
				.ShowEntire()
				.AlignCenter()
				.Text( section.Label )
				.SemiBold().FontSize( 14 ).FontColor( Colors.Grey.Darken3 );

			column.Item().Table( table =>
			{
				table.ColumnsDefinition( columns =>
				{
					for( int i = 0; i < columnHeaders.Count; i++ )
					{
						int padding = (i == 0) ? 20 : 8;
						columns.ConstantColumn( columnHeaders.HeaderWidths[ i ] + padding );
					}
				} );

				for( int i = 0; i < columnHeaders.Count; i++ )
				{
					table.Cell()
					     .Element( PrimaryColumnHeader )
					     .AlignLeft()
					     .Text( columnHeaders.HeaderLabels[ i ] )
					     .SemiBold()
					     .FontColor( Colors.Black );
				}

				foreach( var group in section.Groups )
				{
					// The group header spans all columns
					table
						.Cell()
						.ColumnSpan( columnHeaders.Count )
						.Element( SectionHeader )
						.Text( group.Label )
						.SemiBold();

					int rowIndex = 0;
					foreach( var item in group.Items )
					{
						var background = (rowIndex % 2 != 0) ? Colors.Grey.Lighten4 : Colors.White;
						
						table
							.Cell()
							.Background( background )
							.PaddingLeft( 4, Unit.Point )
							.Text( item.Label );

						foreach( var value in item.Values )
						{
							table
								.Cell()
								.Background( background )
								.PaddingLeft( 4, Unit.Point )
								.Text( value );
						}

						rowIndex += 1;
					}

					// Add some margin space after every group
					table.Cell()
					     .ColumnSpan( columnHeaders.Count )
					     .Height( 6 )
					     .LineHorizontal( 1 )
					     .LineColor( Colors.White );
				}
			});

		} );

		return;

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

	private class ColumnHeaderList
	{
		public required List<string> HeaderLabels { get; init; }
		public required List<uint>   HeaderWidths { get; init; }

		public int TotalWidth
		{
			get
			{
				int total = 0;
				foreach( var width in HeaderWidths )
				{
					total += (int)width;
				}

				return total;
			}
		}

		public uint Count
		{
			get => (uint)HeaderLabels.Count;
		}
	}
}
