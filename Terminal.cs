using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
    public class Terminal
    {
        private const uint COMMAND_COUNT = 1;

        private const float TEXT_WIDTH_PERCENTAGE = 0.025f;
        private const float TEXT_HEIGHT_PERCENTAGE = 0.05f;
        private const float BACKSPACE_DELAY = 75.0f;

        private const string INITIAL_TEXT = "$";

        private readonly string[] COMMANDS =
        {
            "debug"
        };

        private enum Command
        {
            NO_COMMAND = -1,
            DEBUG = 0
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

        public Terminal(Simulator sim)
        {
            context = sim;
            transparency = 0.8f;

            initialText = new Text(sim, INITIAL_TEXT);
            initialText.SetWindowAsDrawingVec(true);
            currentTextPos = new Utils.Vector2(0.0f, 0.0f);
            lines = new Queue<Text>();
            charWidth = TEXT_WIDTH_PERCENTAGE * (float)context.GetWindowWidth();
            charHeight = TEXT_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight();
            lastTime = 0.0f;
            currentTime = 0.0f;
            backspaceDelay = BACKSPACE_DELAY;

            initialText.SetCharBounds(0.0f, (float)context.GetWindowHeight() / 2.0f, charWidth, charHeight, false);
            initialText.Update(null);

            currentTextPos.x = initialText.windowPos.x + initialText.width;
            currentTextPos.y = initialText.windowPos.y;

            textPool = new TextPool(sim);

            Text firstText = textPool.Alloc();
            firstText.SetWindowAsDrawingVec(true);
            firstText.SetCharBounds(currentTextPos.x, currentTextPos.y, charWidth, charHeight, false);
            lines.Enqueue(firstText);
        }

        public void Update(Nullable<Event> sevent)
        {
            initialText.Update(sevent);

            if (Keyboard.IsKeyPressed(Keyboard.Key.BackSpace))
            {
                currentTime = context.GetClock().ElapsedTime.AsMilliseconds();
                if (currentTime - lastTime > backspaceDelay)
                {
                    lines.ToArray()[lines.Count - 1].RemoveCharacter();
                    lastTime = currentTime;
                }
            }
            else
                lastTime = currentTime;

            if (sevent == null)
                return;

            if (sevent.Value.Type == EventType.TextEntered)
            {
                //Check to make sure we can represent the character.
                if (sevent.Value.Text.Unicode >= 32 && sevent.Value.Text.Unicode <= 126)
                    lines.ToArray()[lines.Count - 1].AppendCharacter((char)sevent.Value.Text.Unicode);
            }

            if (sevent.Value.Type == EventType.KeyPressed && sevent.Value.Key.Code == Keyboard.Key.Return)
            {
                Text currentText = null;
                for (uint i = 0; i < lines.Count; i++)
                {
                    currentText = lines.ToArray()[i];
                    currentText.windowPos.y += charHeight;
                }

                //The text on the command line must change it's x coordinate too and possibly be removed.
                lines.ToArray()[lines.Count - 1].windowPos.x -= initialText.width;

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
                    newText.SetWindowAsDrawingVec(true);
                    newText.SetCharBounds(currentTextPos.x, currentTextPos.y, charWidth, charHeight, false);
                    lines.Enqueue(newText);
                }

                InterpretCommand(commandString);
            }
        }

        public void Draw()
        {
            //Draw terminal background.
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4f(0.0f, 0.0f, 0.0f, transparency);

            Gl.glPushMatrix();

            Gl.glTranslatef(0.0f, (float)context.GetWindowHeight() / 2.0f, Utils.DISCARD_Z_POS);
            Gl.glScalef((float)context.GetWindowWidth(), (float)context.GetWindowHeight() / 2.0f, Utils.DISCARD_Z_SCALE);

            Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            Gl.glEnable(Gl.GL_TEXTURE_2D);

            //Draw text.
            initialText.Draw();

            for (uint i = 0; i < lines.Count; i++)
                lines.ToArray()[i].Draw();

            Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private void InterpretCommand(string commandString)
        {
            //Enumerate the command.
            for (uint i = 0; i < COMMAND_COUNT; i++)
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