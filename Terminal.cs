using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
    public class Terminal : Updateable
    {
        private const double TEXT_WIDTH = 96.0;
        private const double TEXT_HEIGHT = 108.0;
        private const double BACKSPACE_DELAY = 75.0;

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

        private double transparency;
        private double charWidth;
        private double charHeight;
        private double backspaceDelay;
        private double lastTime;
        private double currentTime;
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
            transparency = 0.8;

            charWidth = TEXT_WIDTH;
            charHeight = TEXT_HEIGHT;

            initialText = new Text(sim, INITIAL_TEXT);
            initialText.SetTransformUsage(false);
            initialText.SetColor(1.0, 1.0, 1.0);
            initialText.SetCharBounds(0.0, Simulator.WORLD_WINDOW_HEIGHT / 2.0, charWidth, charHeight, false);
            initialText.Update();

            currentTextPos = new Utils.Vector2(0.0, 0.0);
            lines = new Queue<Text>();

            lastTime = 0.0;
            currentTime = 0.0;

            backspaceDelay = BACKSPACE_DELAY;
            command = Command.NO_COMMAND;

            currentTextPos.x = initialText.worldPos.x + initialText.GetWidth();
            currentTextPos.y = initialText.worldPos.y;

            mesh = Simulator.quad;

            textPool = new TextPool(sim);

            Text firstText = textPool.Alloc();
            firstText.SetColor(1.0, 1.0, 1.0);
            firstText.SetTransformUsage(false);
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
                    currentText.worldPos.y += charHeight;
                }

                //The text on the command line must change it's x coordinate too and possibly be removed.
                lines.ToArray()[lines.Count - 1].worldPos.x -= initialText.GetWidth();

                Text frontText = lines.Peek();
                if (frontText.worldPos.y >= Simulator.WORLD_WINDOW_HEIGHT)
                {
                    frontText.SetText("");
                    textPool.Free(frontText);
                    lines.Dequeue();
                }

                string commandString = lines.ToArray()[lines.Count - 1].GetText();

                if (!textPool.Empty())
                {
                    Text newText = textPool.Alloc();
                    newText.SetColor(1.0, 1.0, 1.0);
                    newText.SetTransformUsage(false);
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

            Gl.glColor4d(0.0, 0.0, 0.0, transparency);

            Gl.glLoadIdentity();

            Gl.glPushMatrix();

            Gl.glTranslated(0.0, Simulator.WORLD_WINDOW_HEIGHT / 2.0, Utils.DISCARD_Z_POS);
            Gl.glScaled(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT / 2.0, Utils.DISCARD_Z_SCALE);

            Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            Gl.glEnable(Gl.GL_TEXTURE_2D);

            //Draw text.
            initialText.Draw();

            for (uint i = 0; i < lines.Count; i++)
                lines.ToArray()[i].Draw();

            Gl.glColor4d(1.0, 1.0, 1.0, 1.0);
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