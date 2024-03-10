using Godot;
using System;

public partial class ViewportUpdateStep : SubViewport
{
	private ulong _rendercount = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_rendercount++;

		if(_rendercount % 7  == 0)
		{
            RenderTargetUpdateMode = UpdateMode.Once;
        }


        base._Process(delta);

	}
}
