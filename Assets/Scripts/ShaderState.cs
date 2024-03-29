using System;

public enum ShaderState
{
	None = 0,
	Half = 1,
	Complete = 2,
}

public static class ShaderStateExt {
	public static float toFloat(this ShaderState state)
	{
		switch (state)
		{
			case ShaderState.None:
				return 0.1f;
			case ShaderState.Half:
				return 0.4f;
			case ShaderState.Complete:
				return 1f;
			default:
				throw new ArgumentOutOfRangeException(nameof(state), state, null);
		}
	}
}
