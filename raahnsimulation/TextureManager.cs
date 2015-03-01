using System;
using Tao.OpenGl;

namespace RaahnSimulation
{
	public class TextureManager
	{
		public enum TextureType
		{
			NONE = -1,
			DEFAULT = 0,
			CAR = 0,
			CURSOR_0 = 1,
			CURSOR_1 = 2,
			CHAR_MAP = 3,
			FLAG = 4,
            TRASH = 5,
            BUTTON = 6,
            PANEL = 7
		};

		public const int ROAD_INDEX_OFFSET = 1;

		private string[] TEXTURE_RESOURCES =
		{
			"Data/Textures/TopViewCar.png", "Data/Textures/Cursor0.png", "Data/Textures/Cursor1.png", 
            "Data/Textures/CharMap.png", "Data/Textures/raahn.png", "Data/Textures/Trash.png", 
            "Data/Textures/Button.png", "Data/Textures/Panel.png"
		};

		private const int TEXTURE_COUNT = 8;

        private bool loadedTextures;
		private uint[] textures;
		private TextureType currentTexture;

	    public TextureManager()
	    {
            loadedTextures = false;
			textures = new uint[TEXTURE_COUNT];
	        currentTexture = (TextureType)(TEXTURE_COUNT - 1);
	    }

	    public bool LoadTextures()
	    {
	        Gl.glEnable(Gl.GL_TEXTURE_2D);

	        Gl.glGenTextures(TEXTURE_COUNT, textures);

	        for (int i = 0; i < TEXTURE_COUNT; i++)
	        {
				if (!System.IO.File.Exists(TEXTURE_RESOURCES[i]))
					return false;

                Gdk.Pixbuf currentImage = new Gdk.Pixbuf(TEXTURE_RESOURCES[i]);
                //Flip the image vertically for glTexImage2D.
                currentImage = currentImage.Flip(false);

				Gl.glBindTexture(Gl.GL_TEXTURE_2D, textures[i]);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
				Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
				Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, (int)currentImage.Width, (int)currentImage.Height, 0, 
                                Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, currentImage.Pixels);
	        }

            loadedTextures = true;

	        return true;
	    }

        //Returns whether textures were deleted or not.
        public bool DeleteTextures()
        {
            if (loadedTextures)
                Gl.glDeleteTextures(TEXTURE_COUNT, textures);
            return loadedTextures;
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
