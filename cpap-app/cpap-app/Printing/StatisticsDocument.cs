using System;

using cpap_app.ViewModels;

using cpaplib;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace cpap_app.Printing;

public class StatisticsDocument: IDocument
{
	public UserProfile                Profile   { get; set; }
	public TherapyStatisticsViewModel ViewModel { get; set; }

	public StatisticsDocument( UserProfile user, TherapyStatisticsViewModel viewModel )
	{
		Profile   = user;
		ViewModel = viewModel;
	}
	
	public void Compose( IDocumentContainer document )
	{
		document.Page( page =>
		{
			page.Size( PageSizes.Letter.Landscape() );
			page.Margin( 8, Unit.Point );
			page.PageColor( Colors.White );
			page.DefaultTextStyle( x => x.FontSize( 8 ).FontFamily( Fonts.SegoeUI ) );

			page.Content().Column( column =>
			{
				foreach( var section in ViewModel.Sections )
				{
					ComposeSection( column, section );
				}
			});

			page.Footer()
			    .AlignCenter()
			    .Table( table =>
			    {
				    table.ColumnsDefinition( columns =>
				    {
					    columns.RelativeColumn();
					    columns.RelativeColumn( 3 );
					    columns.RelativeColumn();
				    } );

				    table.Cell().Column( 1 )
				         .Text( x =>
				         {
					         x.Span( "Page " );
					         x.CurrentPageNumber();
					         x.Span( " of " );
					         x.TotalPages();
				         } );

				    table.Cell().Column( 2 )
				         .AlignCenter()
				         .Text( $"Printed on {DateTime.Today:D} at {DateTime.Now:t}" );

				    table.Cell().Column( 3 )
				         .AlignRight()
				         .Text( $"User Profile: {Profile.UserName}" );
			    } );
		} );
	}
	
	private static void ComposeSection( ColumnDescriptor container, TherapyStatisticsSectionViewModel section )
	{
		container.Item().ShowEntire().Column( column =>
		{
			column.Spacing( 8 );
  
			column
				.Item()
				.ShowEntire()
				.AlignCenter()
				.Text( section.Label )
				.SemiBold().FontSize( 12 ).FontColor( Colors.Grey.Darken3 );

			column.Item().Table( table =>
			{
				table.ColumnsDefinition( columns =>
				{
					columns.RelativeColumn( 3 );

					for( int i = 0; i < section.Headers.Count; i++ )
					{
						columns.RelativeColumn();
					}
				} );

				table.Cell().Element( PrimaryColumnHeader ).AlignLeft().Text( "Details" ).SemiBold();

				foreach( var header in section.Headers )
				{
					table.Cell()
					     .Element( PrimaryColumnHeader )
					     .AlignLeft()
					     .Text( header.Label )
					     .SemiBold()
					     .FontColor( Colors.Black );
				}

				foreach( var group in section.Groups )
				{
					table.Cell()
					     .ColumnSpan( (uint)section.Headers.Count + 1 )
					     .Element( SectionHeader )
					     .Text( group.Label )
					     .SemiBold();

					foreach( var item in group.Items )
					{
						table.Cell()
						     .PaddingLeft( 4, Unit.Point )
						     .Text( item.Label );

						foreach( var value in item.Values )
						{
							table.Cell()
							     .PaddingLeft( 4, Unit.Point )
							     .Text( value );
						}
					}

					table.Cell()
					     .ColumnSpan( (uint)section.Headers.Count + 1 )
					     .PaddingLeft( 4, Unit.Point )
					     .Text( string.Empty );
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
}
