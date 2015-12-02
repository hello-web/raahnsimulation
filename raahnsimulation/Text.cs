using System;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class Text : ClickableEntity
	{
        public const double SPACING_WIDTH_PERCENTAGE = 0.8;
        public const double CHAR_DEFAULT_WIDTH = 120.0;
        public const double CHAR_DEFAULT_HEIGHT = 130.0;
        public const double CHAR_MENU_WIDTH = 150.0;
        public const double CHAR_MENU_HEIGHT = 200.0;

		private const int ASCII_OFFSET = 32;

		protected string text;
		private double spacing;
		private double charWidth;
        private double maxLength;
		private Utils.Vector2 charTexPos;
		private Utils.Vector2 charCenter;

	    public Text(Simulator sim, string str) : base(sim)
	    {
	        text = str;
	        spacing = 1.0;
            maxLength = 0.0;

	        texture = TextureManager.TextureType.CHAR_MAP;

            charTexPos = new Utils.Vector2(0.0, 0.0);
            charCenter = new Utils.Vector2(0.0, 0.0);
            //Initially black.
            SetColor(0.0, 0.0, 0.0);
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

	        GL.Color3(color.x, color.y, color.z);

	        for (int i = 0; i < text.Length; i++)
	        {
                if (maxLength > 0.0)
                {
                    double currentWidth = spacing * (i + 1) + charWidth * (1.0 - SPACING_WIDTH_PERCENTAGE);

                    if (currentWidth > maxLength)
                        break;
                }

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
	                charTexPos.x = 0.0;
	                charTexPos.y = 0.0;
	            }

	            charCenter.x = (GetTransformedX() + (i * spacing)) + (charWidth / 2.0);
	            charCenter.y = GetTransformedY() + (height / 2.0);

	            GL.MatrixMode(MatrixMode.Texture);

	            GL.LoadIdentity();
	            GL.Translate((charTexPos.x * Utils.TEXTURE_CHAR_WIDTH), (charTexPos.y * Utils.TEXTURE_CHAR_HEIGHT), 1);
	            GL.Scale(Utils.TEXTURE_CHAR_WIDTH, Utils.TEXTURE_CHAR_HEIGHT, 1);

	            GL.MatrixMode(MatrixMode.Modelview);

	            GL.PushMatrix();

	            GL.Translate(drawingVec.x + (i * spacing), drawingVec.y, Utils.DISCARD_Z_POS);
	            GL.Scale(charWidth, height, Utils.DISCARD_Z_SCALE);
	            GL.DrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

	            GL.PopMatrix();
	        }

	        GL.Color3(1.0, 1.0, 1.0);

	        GL.MatrixMode(MatrixMode.Texture);

	        GL.LoadIdentity();

	        GL.MatrixMode(MatrixMode.Modelview);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

	    public void SetCharBounds(double x, double y, double cWidth, double cHeight, bool fromCenter)
	    {
	        charWidth = cWidth;
	        height = cHeight;
	        spacing = charWidth * SPACING_WIDTH_PERCENTAGE;

            //To get the entire width, we need the complete width of the last char.
	        width = (spacing * text.Length) + charWidth * (1.0 - SPACING_WIDTH_PERCENTAGE);

            aabb.SetSize(width, height);

	        if (fromCenter)
	        {
	            drawingVec.x = x - (width / 2.0);
	            drawingVec.y = y - (height / 2.0);
	        }
	        else
	        {
	            drawingVec.x = x;
	            drawingVec.y = y;
	        }
	    }

        public void SetMaxLength(double length)
        {
            maxLength = length;
        }

	    public void SetText(string newText)
	    {
	        text = newText;
	        width = spacing * text.Length;
            aabb.SetSize(width, height);
	    }

        public void SetColor(double r, double g, double b)
        {
            color.x = r;
            color.y = g;
            color.z = b;
        }

        public new void SetTransformUsage(bool usage)
        {
            base.SetTransformUsage(usage);
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
