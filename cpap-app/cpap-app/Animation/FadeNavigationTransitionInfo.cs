using System;
using System.Threading;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

using FluentAvalonia.UI.Media.Animation;

namespace cpap_app.Animation;

public class FadeNavigationTransitionInfo : NavigationTransitionInfo
{
	public double Duration { get; set; } = 0.67;
	
	public override async void RunAnimation(Animatable ctrl, CancellationToken cancellationToken)
	{
		var animation = new Avalonia.Animation.Animation
		{
			Easing = new SplineEasing( 0.1, 0.9, 0.2, 1.0 ),
			Children =
			{
				new KeyFrame
				{
					Setters =
					{
						new Setter( Visual.OpacityProperty, 0.0 ),
					},
					Cue = new Cue( 0d )
				},
				new KeyFrame
				{
					Setters =
					{
						new Setter( Visual.OpacityProperty, 1.0 ),
					},
					Cue = new Cue( 1d )
				}
			},
			Duration = TimeSpan.FromSeconds( Duration ),
			FillMode = FillMode.Forward
		};

		await animation.RunAsync(ctrl, cancellationToken);

		if( ctrl is Visual visual )
		{
			visual.Opacity = 1.0;
		}
	}
}

