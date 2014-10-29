using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
    public class Terminal : Updateable
    {
        private const float TEXT_WIDTH_PERCENTAGE = 0.025f;
        private const float TEXT_HEIGHT_PERCENTAGE = 0.05f;
        private const float BACKSPACE_DELAY = 75.0f;

        private const string INITIAL_TEXT = "$";

        private static readonly string[] COMMANDS =
        {
            "exit", "debug"
        };

        private enum Command
        {
            NO_COMMAND = -1,
            EXIT = 0,
            DEBUG = 1
        };

        private float transparency;
        private float charWidth;
        private float charHeight;
        private float backspaceDelay;
        private float lastTime;
        private float currentTime;
        private Simulator context;
        private Command command;
        private Text initialText;
        private TextPool textPool;
        private Queue<Text> lines;
        private Utils.Vector2 currentTextPos;
        private Mesh mesh;

        public Terminal(Simulator sim)
        {
            context = sim;
            transparency = 0.8f;

            charWidth = TEXT_WIDTH_PERCENTAGE * (float)context.GetWindowWidth();
            charHeight = TEXT_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight();

            initialText = new Text(sim, INITIAL_TEXT);
            initialText.SetWindowAsDrawingVec(true);
            initialText.SetColor(1.0f, 1.0f, 1.0f);
            initialText.SetCharBounds(0.0f, (float)context.GetWindowHeight() / 2.0f, charWidth, charHeight, false);
            initialText.Update();

            currentTextPos = new Utils.Vector2(0.0f, 0.0f);
            lines = new Queue<Text>();

            lastTime = 0.0f;
            currentTime = 0.0f;

            backspaceDelay = BACKSPACE_DELAY;
            command = Command.NO_COMMAND;

            currentTextPos.x = initialText.windowPos.x + initialText.GetWidth();
            currentTextPos.y = initialText.windowPos.y;

            mesh = Simulator.quad;

            textPool = new TextPool(sim);

            Text firstText = textPool.Alloc();
            firstText.SetColor(1.0f, 1.0f, 1.0f);
            firstText.SetWindowAsDrawingVec(true);
            firstText.SetCharBounds(currentTextPos.x, currentTextPos.y, charWidth, charHeight, false);
            lines.Enqueue(firstText);
        }

        public void Update()
        {
            initialText.Update();

            if (Keyboard.IsKeyPressed(Keyboard.Key.Back))
            {
                currentTime = context.GetStopwatch().ElapsedMilliseconds;
                if (currentTime - lastTime > backspaceDelay)
                {
                    lines.ToArray()[lines.Count - 1].RemoveCharacter();
                    lastTime = currentTime;
                }
            }
            else
                lastTime = currentTime;
        }

        public void UpdateEvent(Event e)
        {
            initialText.UpdateEvent(e);

            if (e.Type == EventType.TextEntered)
            {
                //Check to make sure we can represent the character.
                if (e.Text.Unicode >= 32 && e.Text.Unicode <= 126)
                    lines.ToArray()[lines.Count - 1].AppendCharacter((char)e.Text.Unicode);
            }

            if (e.Type == EventType.KeyPressed && e.Key.Code == Keyboard.Key.Return)
            {
                Text currentText = null;
                for (uint i = 0; i < lines.Count; i++)
                {
                    currentText = lines.ToArray()[i];
                    currentText.windowPos.y += charHeight;
                }

                //The text on the command line must change it's x coordinate too and possibly be removed.
                lines.ToArray()[lines.Count - 1].windowPos.x -= initialText.GetWidth();

                Text frontText = lines.Peek();
                if (frontText.windowPos.y >= (float)context.GetWindowHeight())
                {
                    frontText.SetText("");
                    textPool.Free(frontText);
                    lines.Dequeue();
                }

                string commandString = lines.ToArray()[lines.Count - 1].GetText();

                if (!textPool.Empty())
                {
                    Text newText = textPool.Alloc();
                    newText.SetColor(1.0f, 1.0f, 1.0f);
                    newText.SetWindowAsDrawingVec(true);
                    newText.SetCharBounds(currentTextPos.x, currentTextPos.y, charWidth, charHeight, false);
                    lines.Enqueue(newText);
                }

                ProcessCommand(commandString);
            }
        }

        public void Draw()
        {
            if (!mesh.IsCurrent())
                mesh.MakeCurrent();

            //Draw terminal background.
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4f(0.0f, 0.0f, 0.0f, transparency);

            Gl.glLoadIdentity();

            Gl.glPushMatrix();

            Gl.glTranslatef(0.0f, (float)context.GetWindowHeight() / 2.0f, Utils.DISCARD_Z_POS);
            Gl.glScalef((float)context.GetWindowWidth(), (float)context.GetWindowHeight() / 2.0f, Utils.DISCARD_Z_SCALE);

            Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            Gl.glEnable(Gl.GL_TEXTURE_2D);

            //Draw text.
            initialText.Draw();

            for (uint i = 0; i < lines.Count; i++)
                lines.ToArray()[i].Draw();

            Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private void ProcessCommand(string commandString)
        {
            //Enumerate the command.
            for (int i = 0; i < COMMANDS.Length; i++)
            {
                if (commandString.Equals(COMMANDS[i]))
                {
                    command = (Command)i;
                    break;
                }
            }

            switch (command)
            {
                case Command.NO_COMMAND:
                {
                    break;
                }
                case Command.EXIT:
                {
                    context.running = false;
                    break;
                }
                case Command.DEBUG:
                {
                    if (context.debugging)
                        context.debugging = false;
                    else
                        context.debugging = true;
                    break;
                }
            }

            //Reset command
            command = Command.NO_COMMAND;
        }
    }
}