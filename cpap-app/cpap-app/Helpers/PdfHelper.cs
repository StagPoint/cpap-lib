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
		// Avalonia uses a Point unit that is 1/96th of an inch but PDF uses a Point that is 1/72nd of an inch,
		// so we need to adjust the emSize to make sure that the measured result will match the PDF output at
		// the given font size. 
		const double EM_MULTIPLIER = (1.0 / 72) / (1.0 / 96);
		
		FormattedText formatted = new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			typeFace,
			emSize * EM_MULTIPLIER,
			null
		);

		// The value that is returned is incorrect, because QuestPDF does not provide any easy insight into how
		// text is measured and everything is hidden behind interfaces with internal sealed implementations which
		// means we have to kludge it, but in practice this is close enough to accomplish most of our goals. 
		
		return (float)Math.Ceiling( formatted.Width - text.Length );
	}
}

