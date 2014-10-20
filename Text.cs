using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Text : ClickableEntity
	{
        public const float SPACING_WIDTH_PERCENTAGE = 0.8f;
		private const int ASCII_OFFSET = 32;

		protected string text;
		private float spacing;
		private float charWidth;
		private Utils.Vector2 charTexPos;
		private Utils.Vector2 charCenter;

		public Text()
		{

		}

	    public Text(Simulator sim, string str) : base(sim)
	    {
	        text = str;
	        spacing = 1.0f;
	        texture = TextureManager.TextureType.CHAR_MAP;
            charTexPos = new Utils.Vector2(0.0f, 0.0f);
            charCenter = new Utils.Vector2(0.0f, 0.0f);
            //Initially black.
            SetColor(0.0f, 0.0f, 0.0f);
	    }

	    ~Text()
	    {

	    }

	    public override void Update()
	    {
	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();

	        Gl.glColor3f(color.x, color.y, color.z);

	        for (int i = 0; i < text.Length; i++)
	        {
	            char currentChar = text[i];

                //Is the character within the representable character range.
	            if (currentChar >= 32 && currentChar <= 126)
	            {
                    int index = currentChar - ASCII_OFFSET;
                    charTexPos.y = index / Utils.CHARACTER_TEX_COLUMN_COUNT;
                    charTexPos.x = index - Utils.CHARACTER_TEX_COLUMN_COUNT * (int)charTexPos.y;
                    //Flip y array position because the character used starts from the bottom.
                    charTexPos.y = Utils.CHARACTER_TEX_ROW_COUNT - 1 - charTexPos.y;
	            }
	            else
	            {
	                charTexPos.x = 0.0f;
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
	            Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

	            Gl.glPopMatrix();
	        }

	        Gl.glColor3f(1.0f, 1.0f, 1.0f);

	        Gl.glMatrixMode(Gl.GL_TEXTURE);

	        Gl.glLoadIdentity();

	        Gl.glMatrixMode(Gl.GL_MODELVIEW);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

	    public void SetCharBounds(float x, float y, float cWidth, float cHeight, bool fromCenter)
	    {
	        charWidth = cWidth;
	        height = cHeight;
	        spacing = charWidth * SPACING_WIDTH_PERCENTAGE;

            //To get the entire width, we need the complete width of the last char.
	        width = (spacing * text.Length) + charWidth * 0.2f;

            aabb.SetSize(width, height);

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
	        text = newText;
	        width = spacing * text.Length;
            aabb.SetSize(width, height);
	    }

        public void SetColor(float r, float g, float b)
        {
            color.x = r;
            color.y = g;
            color.z = b;
        }

        public void AppendCharacter(char appendChar)
        {
            text += appendChar;
        }

        public void AppendText(string appendText)
        {
            text += appendText;
        }

        //Remove from the end of the string.
        public void RemoveCharacter()
        {
            if (text.Length > 0)
                text = text.Substring(0, text.Length - 1);
        }

        public string GetText()
        {
            return text;
        }
	}
}
