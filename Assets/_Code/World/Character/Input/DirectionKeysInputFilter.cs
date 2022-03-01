using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [Serializable]
    public class DirectionKeysInputFilter
    {
        public struct Output
        {
            public bool KeyDown;
            public bool Modifier;
            public Vector2 NormalizedOffset;
        }

        public bool Process(DeviceInput inInput, out Output outOutput)
        {
            if (!inInput.IsActive())
            {
                outOutput = default(Output);
                return false;
            }

            Vector2 dir = Vector2.zero;
            if (inInput.KeyDown(KeyCode.W) || inInput.KeyDown(KeyCode.UpArrow)) {
                dir.y += 1;
            }
            if (inInput.KeyDown(KeyCode.S) || inInput.KeyDown(KeyCode.DownArrow)) {
                dir.y -= 1;
            }
            if (inInput.KeyDown(KeyCode.D) || inInput.KeyDown(KeyCode.RightArrow)) {
                dir.x += 1;
            }
            if (inInput.KeyDown(KeyCode.A) || inInput.KeyDown(KeyCode.LeftArrow)) {
                dir.x -= 1;
            }

            if (dir.x != 0 || dir.y != 0) {
                outOutput.KeyDown = true;
                dir.Normalize();

                if (inInput.KeyDown(KeyCode.LeftShift)) {
                    dir *= 0.5f;
                    outOutput.Modifier = true;
                } else {
                    outOutput.Modifier = false;
                }
            } else {
                outOutput.KeyDown = false;
                outOutput.Modifier = inInput.KeyDown(KeyCode.LeftShift);
            }

            outOutput.NormalizedOffset = dir;
            return true;
        }
    }
}