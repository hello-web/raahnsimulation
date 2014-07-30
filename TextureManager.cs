using System;
using Tao.OpenGl;
using SFML.Graphics;

namespace RaahnSimulation
{
	public class TextureManager
	{
		public enum TextureType
		{
			NONE = -1,
			DEFAULT = 0,
			CAR = 0,
			ROAD_0 = 1,
			ROAD_1 = 2,
			CURSOR_0 = 3,
			CURSOR_1 = 4,
			CHAR_MAP = 5,
			FLAG = 6,
			CONE = 7
		};

		public const int ROAD_INDEX_OFFSET = 1;

		private string[] TEXTURE_RESOURCES =
		{
			"Data/Textures/TopViewCar.png", "Data/Textures/Road0.png", "Data/Textures/Road1.png",
			"Data/Textures/Cursor0.png", "Data/Textures/Cursor1.png", "Data/Textures/FreeMonoChars.png",
			"Data/Textures/raahn.png", "Data/Textures/Cone.png"
		};

		private const int TEXTURE_COUNT = 8;

		private uint[] textures;
		private TextureType currentTexture;

	    public TextureManager()
	    {
			textures = new uint[TEXTURE_COUNT];
	        currentTexture = (TextureType)(TEXTURE_COUNT - 1);
	    }

	    ~TextureManager()
	    {
			Gl.glDeleteTextures(TEXTURE_COUNT, textures);
	    }

	    public bool LoadTextures()
	    {
	        Gl.glEnable(Gl.GL_TEXTURE_2D);

	        Gl.glGenTextures(TEXTURE_COUNT, textures);

	        for (int i = 0; i < TEXTURE_COUNT; i++)
	        {
				if (!System.IO.File.Exists(TEXTURE_RESOURCES[i]))
					return false;
				Image currentImage = new Image(TEXTURE_RESOURCES[i]);
                currentImage.FlipVertically();

				Gl.glBindTexture(Gl.GL_TEXTURE_2D, textures[i]);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT);
				SFML.System.Vector2u size = currentImage.Size;
				Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, (int)size.X, (int)size.Y, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, currentImage.Pixels);
	        }

	        return true;
	    }

	    public void SetTexture(TextureType t)
	    {
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, textures[(uint)t]);
	        currentTexture = t;
	    }

		public TextureType GetCurrentTexture()
		{
			return currentTexture;
		}
	}
}
