using UnityEngine.UI;

namespace ChapterRush
{
    internal class RushButton : Button
    {
        private int levelIndex;

        internal void Setup(int levelID)
        {
            this.levelIndex = levelID;
            onClick.AddListener(CallbackEraseButton);
        }

        private void CallbackEraseButton()
        {
            ChapterRush.StartChapterRush(levelIndex);
        }
    }
}
