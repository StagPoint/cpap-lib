using System;
using System.Globalization;

using Avalonia.Media;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace cpap_app.Helpers;

public class PdfHelper
{
	public static float MeasureText( Typeface typeFace, string text, float emSize )
	{
		FormattedText formatted = new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			typeFace,
			emSize,
			null
		);

		return (float)Math.Ceiling( formatted.Width );
	}
}

