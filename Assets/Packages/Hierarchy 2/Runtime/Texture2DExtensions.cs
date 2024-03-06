using System;

using UnityEngine;

namespace Hierarchy2
{
	public static class Texture2DExtensions
	{
		public static string PNGImageEncodeBase64(this Texture2D _texture2D)
		{
			byte[] bytes = _texture2D.EncodeToPNG();
			string base64 = Convert.ToBase64String(bytes);

			return base64;
		}

		public static Texture2D PNGImageDecodeBase64(this string _base64)
		{
			return Convert.FromBase64String(_base64).PNGImageDecode();
		}

		public static Texture2D PNGImageDecode(this byte[] _bytes)
		{
			Texture2D texture2D = new(0, 0, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
		#if UNITY_EDITOR
			texture2D.alphaIsTransparency = true;
		#endif
			texture2D.LoadImage(_bytes);
			texture2D.Apply();

			return texture2D;
		}
	}
}