using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	class Text : ClickableEntity
	{
		private const int LETTER_ASCII_OFFSET = 65;
		private const int NUMBER_ASCII_OFFSET = 48;

		protected string text;
		private int length;
		private float spacing;
		private float charWidth;
		private Utils.Vector2 charTexPos;
		private Utils.Vector2 charCenter;
        private Utils.Vector3 color;

		public Text()
		{

		}

	    public Text(Simulator sim, string str) : base(sim)
	    {
	        text = str;
	        length = str.Length;
	        spacing = 1.0f;
	        texture = TextureManager.TextureType.CHAR_MAP;
            charTexPos = new Utils.Vector2(0.0f, 0.0f);
            charCenter = new Utils.Vector2(0.0f, 0.0f);
            color = new Utils.Vector3(1.0f, 1.0f, 1.0f);
	    }

	    ~Text()
	    {

	    }

	    public override void Update(Nullable<Event> nEvent)
	    {
	        base.Update(nEvent);
	    }

	    public void SetColor(float r, float g, float b)
	    {
	        color.x = r;
	        color.y = g;
	        color.z = b;
	    }

	    public override void Draw()
	    {
	        base.Draw();

	        Gl.glColor3f(color.x, color.y, color.z);

	        for (int i = 0; i < length; i++)
	        {
	            char currentChar = text[i];

	            //is it a letter, number, or space
	            if (currentChar >= 65 && currentChar <= 90)
	            {
	                charTexPos.x = Utils.LETTER_POSITIONS[currentChar - LETTER_ASCII_OFFSET].x;
	                charTexPos.y = Utils.LETTER_POSITIONS[currentChar - LETTER_ASCII_OFFSET].y;
	            }
	            else if (currentChar >= 48 && currentChar <= 57)
	            {
	                charTexPos.x = Utils.NUMBER_POSITIONS[currentChar - NUMBER_ASCII_OFFSET].x;
	                charTexPos.y = Utils.NUMBER_POSITIONS[currentChar - NUMBER_ASCII_OFFSET].y;
	            }
	            else if (currentChar == 32)
	            {
	                charTexPos.x = 3.0f;
	                charTexPos.y = 0.0f;
	            }

	            charCenter.x = (worldPos.x + (i * spacing)) + (charWidth / 2.0f);
	            charCenter.y = worldPos.y + (height / 2.0f);

	            Gl.glMatrixMode(Gl.GL_TEXTURE);

	            Gl.glLoadIdentity();
	            Gl.glTranslatef((charTexPos.x * Utils.TEXTURE_CHAR_WIDTH), (charTexPos.y * Utils.TEXTURE_CHAR_HEIGHT), 1);
	            Gl.glScalef(Utils.TEXTURE_CHAR_WIDTH, Utils.TEXTURE_CHAR_HEIGHT, 1);

	            Gl.glMatrixMode(Gl.GL_MODELVIEW);

	            Gl.glPushMatrix();

	            Gl.glTranslatef(drawingVec.x + (i * spacing), drawingVec.y, Utils.DISCARD_Z_POS);
	            Gl.glScalef(charWidth, height, Utils.DISCARD_Z_SCALE);
	            Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

	            Gl.glPopMatrix();
	        }

	        Gl.glColor3f(1.0f, 1.0f, 1.0f);

	        Gl.glMatrixMode(Gl.GL_TEXTURE);

	        Gl.glLoadIdentity();

	        Gl.glMatrixMode(Gl.GL_MODELVIEW);
	    }

	    public void SetCharBounds(float x, float y, float cWidth, float cHeight, bool fromCenter)
	    {
	        charWidth = cWidth;
	        height = cHeight;
	        spacing = charWidth * 0.8f;
	        width = spacing * length;
	        if (fromCenter)
	        {
	            drawingVec.x = x - (width / 2.0f);
	            drawingVec.y = y - (height / 2.0f);
	        }
	        else
	        {
	            drawingVec.x = x;
	            drawingVec.y = y;
	        }
	    }

	    public void SetText(string newText)
	    {
	        int newLength = 0;
	        text = newText;
	        newLength = text.Length;
	        length = newLength;
	        width = spacing * length;
	    }
	}
}
