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

		// The value that is returned is incorrect, because QuestPDF does not provide any easy insight into how
		// text is measured and everything is hidden behind interfaces with internal sealed implementations which
		// means we have to kludge it, but in practice this is close enough to accomplish most of our goals. 

		return (float)Math.Ceiling( formatted.Width );
	}
}

