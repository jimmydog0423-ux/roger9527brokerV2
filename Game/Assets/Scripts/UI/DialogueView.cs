using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>
    /// 劇情與教學對話：打字效果、角色頭像、聚光提示框、標題卡。
    /// 對話出現時會擋住下面的操作；點一下顯示全文，再點一下繼續。
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        RectTransform rt;
        GameRoot game;
        Image storyBg;
        readonly RectTransform[] dims = new RectTransform[4];
        RectTransform hiFrame;
        Image hiImg;
        RectTransform blocker;
        RectTransform bigPortraitBox;
        Image bigPortrait;
        RectTransform box;
        RectTransform portraitBox;
        Image portrait;
        RectTransform nameBox;
        Image nameBg;
        Text nameText;
        Text body;
        Text more;
        RectTransform card;
        Image cardBg;
        Text cardTitle, cardSub, cardMore;

        string full = "";
        int shown;
        float acc;
        bool typing;
        bool cardMode;
        Action done;
        string sayFocus;
        string waitFocus;
        bool storyMode;
        float blink;

        public bool Busy { get { return done != null; } }

        public static DialogueView Build(RectTransform root, GameRoot game)
        {
            RectTransform r = Px.Rect(root, "Dialogue", 0, 0, 640, 360);
            DialogueView v = r.gameObject.AddComponent<DialogueView>();
            v.rt = r;
            v.game = game;
            v.BuildUI();
            return v;
        }

        void BuildUI()
        {
            storyBg = Px.Fill(rt, 0, 0, 640, 360, Pal.Hex("#0d0b14"));
            storyBg.gameObject.SetActive(false);

            for (int i = 0; i < 4; i++)
            {
                Image d = Px.Fill(rt, 0, 0, 1, 1, Pal.Alpha(Pal.Ink, 160));
                dims[i] = (RectTransform)d.transform;
                d.gameObject.SetActive(false);
            }
            hiImg = Px.Frame(rt, 0, 0, 10, 10, Pal.Orange, 2);
            hiFrame = (RectTransform)hiImg.transform;
            hiFrame.gameObject.SetActive(false);

            blocker = Px.Rect(rt, "Blocker", 0, 0, 640, 360);
            Image bi = blocker.gameObject.AddComponent<Image>();
            bi.color = Pal.Clear;
            bi.raycastTarget = true;
            Clickable c = blocker.gameObject.AddComponent<Clickable>();
            c.onClick = OnClick;
            blocker.gameObject.SetActive(false);

            bigPortraitBox = ConsoleArt.Panel(rt, "BigPortrait", 30, 138, 100, 116, Pal.VideoWall, Pal.Ink);
            bigPortrait = PortraitArt.Draw(bigPortraitBox, "guidong", 2, 2, 4);
            ConsoleArt.FrameCamera(bigPortraitBox, 0, 0, 100, 116);
            bigPortraitBox.gameObject.SetActive(false);

            box = ConsoleArt.Panel(rt, "Box", 8, 290, 624, 66, Pal.Plum, Pal.PlumLight);
            portraitBox = ConsoleArt.Panel(box, "PortraitBox", 6, 5, 50, 58, Pal.VideoWall, Pal.Ink);
            portrait = PortraitArt.Draw(portraitBox, "guidong", 1, 1, 2);
            ConsoleArt.FrameCamera(portraitBox, 0, 0, 50, 58);
            nameBg = Px.Fill(box, 62, 4, 80, 14, Pal.Orange);
            nameText = Px.Label(box, "", 62, 4, 80, 14, Pal.Ink, TextAnchor.UpperCenter, false);
            nameBox = (RectTransform)nameBg.transform;
            RectTransform badgeShine = Px.Fill(nameBox, 0, 0, 80, 1, Pal.Alpha(Pal.Cream, 155)).rectTransform;
            badgeShine.anchorMax = Vector2.one;
            badgeShine.offsetMin = new Vector2(1, -1);
            badgeShine.offsetMax = new Vector2(-1, 0);
            body = Px.Label(box, "", 62, 20, 556, 42, Pal.Cream, TextAnchor.UpperLeft, true);
            more = Px.Label(box, "點一下繼續", 540, 50, 80, 14, Pal.Lav, TextAnchor.UpperRight, false);
            box.gameObject.SetActive(false);

            card = Px.Rect(rt, "Card", 0, 0, 640, 360);
            cardBg = card.gameObject.AddComponent<Image>();
            cardBg.color = Pal.Hex("#0d0b14");
            cardBg.raycastTarget = false;
            RectTransform plate = ConsoleArt.Panel(card, "ChapterPlate", 32, 119, 576, 100, Pal.PlumDeep, Pal.PlumLight);
            Px.Fill(plate, 264, 50, 48, 1, Pal.Orange);
            Px.Fill(plate, 283, 52, 10, 1, Pal.TealLight);
            cardTitle = Px.Label(card, "", 0, 136, 640, 28, Pal.Cream, TextAnchor.UpperCenter, false);
            cardTitle.fontSize = 24;
            cardSub = Px.Label(card, "", 40, 176, 560, 28, Pal.Lav, TextAnchor.UpperCenter, true);
            RectTransform prompt = ConsoleArt.Panel(card, "ContinuePlate", 270, 311, 100, 23, Pal.PlumDeep, Pal.PlumLight);
            cardMore = Px.Label(prompt, "點一下繼續", 0, 5, 100, 14, Pal.Lav, TextAnchor.UpperCenter, false);
            card.gameObject.SetActive(false);
        }

        // ---------- 對外 ----------

        public void SetStory(string bg)
        {
            storyMode = !string.IsNullOrEmpty(bg) && bg != "none";
            storyBg.gameObject.SetActive(storyMode);
            storyBg.sprite = null;
            if (bg == "black")
            {
                storyBg.color = Pal.Hex("#0d0b14");
                if (NightArt.Paint(storyBg, "story")) storyBg.color = Pal.Hex("#c6bad4");
            }
            else
            {
                SceneMotion.Attach(storyBg, null);
                if (bg == "office") storyBg.color = Pal.Alpha(Pal.Ink, 120);
            }
        }

        public void Say(string who, string name, string text, string focus, Action onDone)
        {
            cardMode = false;
            card.gameObject.SetActive(false);
            done = onDone;
            sayFocus = focus;
            full = text ?? "";
            shown = 0;
            acc = 0f;
            typing = true;
            body.text = "";
            bool hasWho = !string.IsNullOrEmpty(who);
            nameText.text = name ?? "";
            int nw = Px.TextWidth(name) + 12;
            nameBox.sizeDelta = new Vector2(nw, 14);
            ((RectTransform)nameText.transform).sizeDelta = new Vector2(nw, 14);
            nameBg.gameObject.SetActive(!string.IsNullOrEmpty(name));
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(name));

            if (storyMode)
            {
                Px.Place(box, 16, 266, 608, 86);
                portraitBox.gameObject.SetActive(false);
                bigPortraitBox.gameObject.SetActive(hasWho);
                if (hasWho) bigPortrait.sprite = PortraitArt.Get(who);
                Px.Move(nameBox, 10, 6);
                Px.Move((RectTransform)nameText.transform, 10, 6);
                float by = string.IsNullOrEmpty(name) ? 10 : 24;
                Px.Place((RectTransform)body.transform, 12, by, 584, 56);
                Px.Place((RectTransform)more.transform, 520, 70, 80, 14);
            }
            else
            {
                bigPortraitBox.gameObject.SetActive(false);
                Rect fr;
                bool top = false;
                if (!string.IsNullOrEmpty(focus) && game.TryFocus(focus, out fr)) top = fr.yMax > 284;
                Px.Place(box, 8, top ? 4 : 290, 624, 66);
                portraitBox.gameObject.SetActive(hasWho);
                if (hasWho) portrait.sprite = PortraitArt.Get(who);
                float x = hasWho ? 62 : 10;
                Px.Move(nameBox, x, 4);
                Px.Move((RectTransform)nameText.transform, x, 4);
                float by = string.IsNullOrEmpty(name) ? 6 : 20;
                Px.Place((RectTransform)body.transform, x, by, 614 - x, 42);
                Px.Place((RectTransform)more.transform, 540, 50, 80, 14);
            }
            box.gameObject.SetActive(true);
            blocker.gameObject.SetActive(true);
            blocker.SetAsLastSibling();
            bigPortraitBox.SetAsLastSibling();
            box.SetAsLastSibling();
            more.gameObject.SetActive(false);
            if (Settings.TextSpeed >= 3) FinishTyping();
        }

        public void Card(string title, string sub, Action onDone)
        {
            cardMode = true;
            done = onDone;
            sayFocus = null;
            box.gameObject.SetActive(false);
            bigPortraitBox.gameObject.SetActive(false);
            cardTitle.text = title ?? "";
            cardSub.text = sub ?? "";
            cardBg.color = storyMode ? Pal.Clear : Pal.Alpha(Pal.Hex("#0d0b14"), 230);
            card.gameObject.SetActive(true);
            blocker.gameObject.SetActive(true);
            card.SetAsLastSibling();
            blocker.SetAsLastSibling();
        }

        public void SetWaitFocus(string key)
        {
            waitFocus = key;
        }

        public void HideAll()
        {
            done = null;
            sayFocus = null;
            waitFocus = null;
            box.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            bigPortraitBox.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++) dims[i].gameObject.SetActive(false);
            hiFrame.gameObject.SetActive(false);
            SetStory("none");
        }

        // ---------- 內部 ----------

        void FinishTyping()
        {
            shown = full.Length;
            body.text = full;
            typing = false;
            more.gameObject.SetActive(true);
        }

        void OnClick()
        {
            if (done == null) return;
            if (!cardMode && typing)
            {
                FinishTyping();
                return;
            }
            Action d = done;
            done = null;
            sayFocus = null;
            box.gameObject.SetActive(false);
            card.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            bigPortraitBox.gameObject.SetActive(false);
            d();
        }

        void Update()
        {
            if (done != null && !cardMode && typing)
            {
                acc += Time.unscaledDeltaTime * Settings.CharsPerSecond;
                int n = Mathf.Min(full.Length, Mathf.FloorToInt(acc));
                if (n != shown)
                {
                    shown = n;
                    body.text = full.Substring(0, shown);
                }
                if (shown >= full.Length) FinishTyping();
            }
            if (done != null && !cardMode)
            {
                blink += Time.unscaledDeltaTime;
                more.color = (blink % 1f) < 0.6f ? (Color)(Color32)Pal.Lav : (Color)(Color32)Pal.PlumLight;
            }
            UpdateFocus();
        }

        void UpdateFocus()
        {
            string key = done != null ? (cardMode ? null : sayFocus) : waitFocus;
            Rect fr = new Rect();
            bool has = !string.IsNullOrEmpty(key) && game.TryFocus(key, out fr);
            bool spot = has && done != null;
            for (int i = 0; i < 4; i++) dims[i].gameObject.SetActive(spot);
            hiFrame.gameObject.SetActive(has);
            if (!has) return;
            float x0 = Mathf.Max(0, fr.x - 2), y0 = Mathf.Max(0, fr.y - 2);
            float x1 = Mathf.Min(640, fr.xMax + 2), y1 = Mathf.Min(360, fr.yMax + 2);
            if (spot)
            {
                Px.Place(dims[0], 0, 0, 640, y0);
                Px.Place(dims[1], 0, y1, 640, 360 - y1);
                Px.Place(dims[2], 0, y0, x0, y1 - y0);
                Px.Place(dims[3], x1, y0, 640 - x1, y1 - y0);
            }
            Px.Place(hiFrame, x0, y0, x1 - x0, y1 - y0);
            blink += done == null ? Time.unscaledDeltaTime : 0f;
            hiImg.color = (blink % 0.8f) < 0.5f ? (Color)(Color32)Pal.Orange : (Color)(Color32)Pal.Yellow;
            if (done == null) hiFrame.SetAsLastSibling();
        }
    }
}
