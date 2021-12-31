//#define ScriptableScrollRect_Test

using UnityEngine;
using UnityEngine.UI;
using Transient.SimpleContainer;

namespace Transient.UI {
    public sealed class ScrollDataView : ScrollRect {
        ScrollData data { get; set; }
        public void Reset(ScrollData data_) => data = data_;

        public float ContentPos(float xScale, float yScale) => content.anchoredPosition.x * xScale + content.anchoredPosition.y * yScale;

        protected override void LateUpdate() {
            base.LateUpdate();
            if(data == null || data.posFixed)
                return;
            data.Refresh();
        }

#if ScriptableScrollRect_Test
        private void OnGUI() {
            if(GUI.Button(new Rect(100, 100, 100, 50), "scrollto")) {
                ds.ScrollTo(23);
            }
        }
#endif
    }

    public class Entry<T> {
        public RectTransform transform;
        public T item;
        public EntryModifier info;
    }

    public struct EntryModifier {
        public float size;
    }

    public struct ScrollToTarget {
        public const float InstantTime = 0.001f;
        public float scrollTarget, scrollStart;
        public float scrollTime, scrollDurationInverse;

        public void Target(float start, float target, float duration = InstantTime) {
            Debug.Log(start + " to " + target);
            scrollStart = start;
            scrollTarget = target;
            scrollTime = duration;
            scrollDurationInverse = 1f / duration;
        }

        public float TargetStep(float dt) {
            scrollTime -= dt;
            return Mathf.Lerp(scrollStart, scrollTarget, 1 - scrollTime * scrollDurationInverse);
        }
    }

    public abstract class ScrollData {
        public bool posFixed;
        public bool vertical = true;
        public bool autoCenter = false;

        public abstract void Refresh();
        public abstract void Fill(float contentPos);
        public abstract void ScrollTo(int id, float duration = ScrollToTarget.InstantTime);
        public abstract void SnapTo(int id);
    }

    public sealed class ScrollDataFlex<T> : ScrollData where T : new() {
        public const float DefaultRecyclePadding = 5;
        ScrollDataView _scroll;
        Vector2 defaultElementSize;
        int elementPerLine;
        Vector2 elementPositionUnit;
        int targetCount, currentCount;
        public bool loop { get; set; }//TODO: loop data
        private StorageList<Entry<T>> entry;
        Func<RectTransform, T> PrepareT;
        Func<int, T, EntryModifier> FillT;
        Action<T, bool> SwitchT;
        RectTransform template;
        int indexOffset, tailCount;
        public float headPadding, tailPadding, recyclePadding;
        float viewportSize, paddedSize;
        float headOffset;
        float currentContentSize;
        Vector2 fixedOffset;
        Vector2 contentOffset;
        Func<int, int, Vector2, Vector2> Reposition;
        Vector2 flexibleSize, fixedSize, restPos;
        float xScale, yScale;
        ScrollToTarget scrollToTarget;

        public ScrollDataFlex<T> Init(ScrollDataView scroll_, Func<RectTransform, T> PrepareT_, Action<T, bool> SwitchT_) {
            _scroll = scroll_;
            elementPerLine = 1;
            headPadding = 0;
            tailPadding = 0;
            recyclePadding = DefaultRecyclePadding;
            SwitchT = SwitchT_;
            PrepareT = PrepareT_;
            template = (RectTransform)_scroll.content.Find("entry");
            template.gameObject.SetActive(false);
            defaultElementSize = template.sizeDelta;
            entry = new StorageList<Entry<T>>(128, 16, InitEntry);
            var e = PrepareT(template);
            entry.Add(new Entry<T>() {
                transform = template,
                item = e
            });
            return this;
        }

        public ScrollDataFlex<T> Repurpose(Func<int, T, EntryModifier> FillT_, Func<int, int, Vector2, Vector2> ReposT_ = null) {
            Reposition = ReposT_ ?? RepositionDefault;
            FillT = FillT_;
            _scroll.Reset(this);
            return this;
        }

        public ScrollDataFlex<T> Padding(float headPadding_, float tailPadding_, float recyclePadding_ = DefaultRecyclePadding) {
            headPadding = headPadding_;
            tailPadding = tailPadding_;
            recyclePadding = recyclePadding_;
            return this;
        }

        public ScrollDataFlex<T> Offset(float oftX_, float oftY_) {
            contentOffset = new Vector2(oftX_, oftY_);
            return this;
        }

        public ScrollDataFlex<T> Resize(int elementPerLine_ = 1, float visibleSize_ = -1f, float elementFlexibleDirSize_ = 0, float elementFixedDirSize_ = 0) {
            elementPerLine = elementPerLine_;
            if (visibleSize_ >= 0)
                ((RectTransform)_scroll.transform).sizeDelta = flexibleSize * visibleSize_;
            if (vertical) {
                defaultElementSize.y = elementFlexibleDirSize_ > 0 ? elementFlexibleDirSize_ : defaultElementSize.y;
                defaultElementSize.x = elementFixedDirSize_ > 0 ? elementFixedDirSize_ : defaultElementSize.x;
            }
            else {
                defaultElementSize.x = elementFlexibleDirSize_ > 0 ? elementFlexibleDirSize_ : defaultElementSize.x;
                defaultElementSize.y = elementFixedDirSize_ > 0 ? elementFixedDirSize_ : defaultElementSize.y;
            }
            return this;
        }

        public override void Refresh() {
            if(scrollToTarget.scrollTime > 0) {
                var target = scrollToTarget.TargetStep(Time.deltaTime);
                Fill(-target);
                _scroll.content.anchoredPosition = -flexibleSize * target + restPos;
                _scroll.velocity = Vector2.zero;
            }
            else if (_scroll.velocity != Vector2.zero) {
                //when scrolling vertical (y-axis), assume negative = head direction
                Fill(_scroll.ContentPos(xScale, -yScale));
            }
        }

        public override void Fill(float contentPos) {
            float psDiff = contentPos + headOffset;
            float peDiff = psDiff + currentContentSize - paddedSize;
            float psEntryOffset = entry[0].info.size;
            float peEntryOffset = entry[entry.Count-1].info.size;
            int totalCount = indexOffset + currentCount;
            bool sizeChanged = false;
            Entry<T> e = null;
            //Debug.Log($"posOffset {contentPosOffset} psDiff {psDiff} ps {psEntryOffset}");
            if(totalCount == targetCount && contentPos < 0) {
                //nothing
            }
            else if(psDiff < 0 && -psDiff >= psEntryOffset + recyclePadding) {
                //Debug.Log($"recycle head psDiff {psDiff} ps {psEntryOffset}");
                //recycle head
                indexOffset += elementPerLine;
                currentCount -= elementPerLine;
                for(int r = 0;r < elementPerLine;++r) {
                    SwitchT(entry[r].item, false);
                }
                entry.RemoveAt(0, elementPerLine);
                headOffset += psEntryOffset;
                currentContentSize -= psEntryOffset;
            }
            else if(psDiff > 0 && indexOffset > 0) {
                //Debug.Log($"add head psDiff {psDiff} ps {psEntryOffset}");
                //add head
                indexOffset -= elementPerLine;
                currentCount += elementPerLine;
                entry.AddTo(0, elementPerLine);
                for(int r = 0;r < elementPerLine;++r) {
                    e = ActivateEntry(entry[r]);
                    e.info = FillT(indexOffset+r, e.item);
                    e.transform.anchoredPosition = Repos(r, -e.info.size-currentContentSize);
                    e.transform.name = (indexOffset+r).ToString();
                }
                headOffset -= e.info.size;
                currentContentSize += e.info.size;
            }
            if(indexOffset == 0 && contentPos > 0) {
                //nothing
            }
            else if(peDiff > 0 && peDiff >= peEntryOffset + recyclePadding) {
                //Debug.Log($"recycle tail peDiff {peDiff} pe {peEntryOffset}");
                sizeChanged = true;
                //recycle tail
                for(int r = 0;r < tailCount;++r) {
                    SwitchT((e = entry.RemoveAt(entry.Count-1)).item, false);
                }
                //Debug.Log($"recycled {tailCount}");
                currentCount -= tailCount;
                tailCount = elementPerLine;
                currentContentSize -= peEntryOffset;
            }
            else if(peDiff < 0 && totalCount < targetCount) {
                //Debug.Log($"add tail peDiff {peDiff} pe {peEntryOffset}");
                sizeChanged = true;
                //add tail
                tailCount = Mathf.Min(targetCount - totalCount, elementPerLine);
                for(int r = 0;r < tailCount;++r) {
                    e = ActivateEntry(entry.Add());
                    e.info = FillT(totalCount+r, e.item);
                    e.transform.anchoredPosition = Repos(r, 0);
                    e.transform.name = (totalCount+r).ToString();
                }
                //Debug.Log($"added {tailCount} {totalCount} {targetCount} {e.offset}");
                currentCount += tailCount;
                currentContentSize += e.info.size;
            }
            if(sizeChanged) {
                RefreshContentSize();
            }
        }

        public override void ScrollTo(int id, float duration = ScrollToTarget.InstantTime) {
            float offset = 0;
            for(int r = 0;r < id;r += elementPerLine) {
                offset += entry[r].info.size;
            }
            scrollToTarget.Target(_scroll.ContentPos(xScale, -yScale), offset, duration);
        }

        public override void SnapTo(int id) {
            //TODO
        }

        private void RefreshContentSize() {
            var size = headOffset + currentContentSize;
            _scroll.content.sizeDelta = size * flexibleSize;
            if (autoCenter) {
                var viewSize = Vector2.Dot(_scroll.viewport.rect.size, flexibleSize);
                _scroll.content.pivot = size > viewSize ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
            }
        }

        Vector2 RepositionDefault(int u_, int elementPerLine_, Vector2 elementOffset_) {
            return elementOffset_ * (u_ % elementPerLine_);
        }

        //assume no pivot offset
        Vector2 Repos(int u_, float offset_) {
            //Debug.Log($"pos {u} {elementPerLine} {u/elementPerLine}*{lineOffset}+{u%elementPerLine}*{elementOffset}");
            return contentOffset + (headOffset + currentContentSize + offset_) * flexibleSize + Reposition(u_, elementPerLine, elementPositionUnit);
        }

        private Entry<T> InitEntry() {
            var entry = new Entry<T>();
            RectTransform element = (RectTransform)GameObject.Instantiate<GameObject>(template.gameObject, _scroll.content, false).transform;
            entry.transform = element;
            entry.item = PrepareT(element);
            return entry;
        }

        private Entry<T> ActivateEntry(Entry<T> e_) {
            e_.transform.sizeDelta = defaultElementSize;
            SwitchT(e_.item, true);
            return e_;
        }

        public void Refill() {
            currentContentSize = 0;
            currentCount = 0;
            foreach(var y in entry) {
                SwitchT(y.item, false);
            }
            entry.RemoveAll();
            Entry<T> e;
            int t = 0, k = 0;
            paddedSize = headPadding;
            //TODO head padding

            paddedSize += viewportSize;
            while(t < targetCount && currentContentSize < paddedSize) {
                e = ActivateEntry(entry.Add());
                e.info = FillT(t+indexOffset, e.item);
                e.transform.anchoredPosition = Repos(t, 0);
                ++t;
                ++currentCount;
                if(++k == elementPerLine) {
                    currentContentSize += e.info.size;
                    k = 0;
                }
            }
            paddedSize += tailPadding;
            //tail padding
            while(t < targetCount && currentContentSize < paddedSize) {
                //TODO same code
                e = ActivateEntry(entry.Add());
                e.info = FillT(t+indexOffset, e.item);
                e.transform.anchoredPosition = Repos(t, 0);
                ++t;
                ++currentCount;
                if(++k == elementPerLine) {
                    currentContentSize += e.info.size;
                    k = 0;
                }
            }
            posFixed = t == targetCount && currentContentSize <= viewportSize;
            tailCount = k == 0 ? elementPerLine : k;
            RefreshContentSize();
        }

        public void Reset() {
            _scroll.StopMovement();
            indexOffset = 0;
            headOffset = 0;
            _scroll.content.anchoredPosition = restPos;
            Refill();
        }

        public void Reset(int count_) {
            if(vertical) {
                flexibleSize = Vector2.up;
                fixedSize = new Vector2(_scroll.content.sizeDelta.x, 0);
                restPos = new Vector2(_scroll.content.anchoredPosition.x, 0);
                viewportSize = _scroll.viewport.rect.height;
                elementPositionUnit = new Vector2(defaultElementSize.x, 0);
                xScale = 0;
                yScale = 1;
            }
            else {
                flexibleSize = Vector2.right;
                fixedSize = new Vector2(0, _scroll.content.sizeDelta.y);
                restPos = new Vector2(0, _scroll.content.anchoredPosition.y);
                viewportSize = _scroll.viewport.rect.width;
                elementPositionUnit = new Vector2(0, -defaultElementSize.y);
                xScale = 1;
                yScale = 0;
            }
            targetCount = count_;
            Reset();
        }

        public T Ref(int u) {
            return entry[u].item;
        }
    }

    public sealed class ScrollDataFixed<T> : ScrollData where T : new() {
        ScrollDataView _scroll;
        int LineScrollRange { get; set; }
        Vector2 defaultElementSize;
        int elementPerLine;
        Vector2 pivotOffset;
        Vector2 elementOffset;
        int targetCount;
        int entryCount;
        public bool Loop { get; set; }//TODO: loop data
        private StorageList<Entry<T>> entry;
        Func<RectTransform, T> PrepareT;
        Action<int, T> FillT;
        EntryModifier modifier;
        Action<T, bool> SwitchT;
        RectTransform template;
        int ps, pe, lineCountViewport, indexOffset;
        float elementSize, elementSizeInverse, viewportSize;
        Vector2 contentOffset;
        Vector2 lineOffset;
        Vector2 flexibleSize, fixedSize, restPos;
        Func<int, int, Vector2, Vector2, Vector2> Reposition;
        float xScale, yScale;
        ScrollToTarget scrollToTarget;

        public ScrollDataFixed<T> Init(ScrollDataView scroll_, Func<RectTransform, T> PrepareT_, Action<T, bool> SwitchT_, string templateName_ = "entry") {
            _scroll = scroll_;
            elementPerLine = 1;
            PrepareT = PrepareT_;
            SwitchT = SwitchT_;
            template = (RectTransform)_scroll.content.Find(templateName_);
            template.gameObject.SetActive(false);
            defaultElementSize = template.sizeDelta;
            entry = new StorageList<Entry<T>>(64, 16, InitEntry);
            var e = PrepareT(template);
            entry.Add(new Entry<T>() {
                transform = template,
                item = e
            });
            return this;
        }

        public ScrollDataFixed<T> Repurpose(Action<int, T> FillT_, Func<int, int, Vector2, Vector2, Vector2> ReposT_ = null) {
            Reposition = ReposT_ ?? RepositionDefault;
            FillT = FillT_;
            _scroll.Reset(this);
            return this;
        }

        public ScrollDataFixed<T> Resize(int elementPerLine_ = 1, float visibleSize_ = -1f, float elementFlexibleDirSize_ = 0, float elementFixedDirSize_ = 0) {
            elementPerLine = elementPerLine_;
            if (visibleSize_ >= 0) {
                var rect = (RectTransform)_scroll.transform;
                rect.anchoredPosition = restPos;
                rect.sizeDelta = flexibleSize * visibleSize_ + fixedSize;
            }
            if (vertical) {
                defaultElementSize.y = elementFlexibleDirSize_ > 0 ? elementFlexibleDirSize_ : defaultElementSize.y;
                defaultElementSize.x = elementFixedDirSize_ > 0 ? elementFixedDirSize_ : defaultElementSize.x;
                elementOffset = new Vector2(defaultElementSize.x, 0);
                lineOffset = new Vector2(0, -defaultElementSize.y);
                elementSize = defaultElementSize.y;
            }
            else {
                defaultElementSize.x = elementFlexibleDirSize_ > 0 ? elementFlexibleDirSize_ : defaultElementSize.x;
                defaultElementSize.y = elementFixedDirSize_ > 0 ? elementFixedDirSize_ : defaultElementSize.y;
                elementOffset = new Vector2(0, -defaultElementSize.y);
                lineOffset = new Vector2(defaultElementSize.x, 0);
                elementSize = defaultElementSize.x;
            }
            elementSizeInverse = 1f / elementSize;
            return this;
        }

        public override void Refresh() {
            if(scrollToTarget.scrollTime > 0) {
                var target = scrollToTarget.TargetStep(Time.deltaTime);
                Fill(-target);
                _scroll.content.anchoredPosition = -flexibleSize * target + restPos;
                _scroll.velocity = Vector2.zero;
            }
            else if(_scroll.velocity != Vector2.zero) {
                //when scrolling vertical (y-axis), assume negative = head direction
                Fill(_scroll.ContentPos(xScale, -yScale));
            }
        }

        public override void Fill(float contentPos) {
            int k = Mathf.FloorToInt(-contentPos * elementSizeInverse) * elementPerLine;
            if(k < 0) k = 0;
            else if(k > targetCount) k = targetCount;
            if(indexOffset == k)
                return;
            //Debug.Log ("target offset  = " + k);
            int m = k - indexOffset;
            Entry<T> e;
            if(m>0) {
                for(int y = 0;y < m;++y) {
                    e = entry[ps];
                    int lo = ++indexOffset + entryCount - 1;
                    //Debug.Log ("Fill " + m + " to " + lo + " ps=" + ps);
                    if(lo < targetCount) {
                        e.transform.anchoredPosition = Repos(lo);
                        FillT(lo, e.item);
                        SwitchT(e.item, true);
                    }
                    else {
                        SwitchT(e.item, false);
                    }
                    ps = (ps + 1)%entryCount;
                }
                pe = (pe + m)%entryCount;
            }
            else {
                for(int y = 0;y > m;--y) {
                    e = entry[pe];
                    --indexOffset;
                    //Debug.Log ("Fill " + m + " to " + offset + " pe = " + pe);
                    e.transform.anchoredPosition = Repos(indexOffset);
                    FillT(indexOffset, e.item);
                    SwitchT(e.item, true);
                    pe = (pe - 1 + entryCount)%entryCount;
                }
                ps = (ps + m + entryCount)%entryCount;
            }
        }

        public override void ScrollTo(int k, float duration = ScrollToTarget.InstantTime) {
            float offset = Mathf.FloorToInt((float)k / elementPerLine) * elementSize;
            scrollToTarget.Target(_scroll.ContentPos(xScale, -yScale), offset, duration);
        }

        public override void SnapTo(int id) {
            //TODO
        }

        private void RefreshContentSize(float size) {
            _scroll.content.sizeDelta = size * flexibleSize + fixedSize;
            if (autoCenter) {
                var viewSize = Vector2.Dot(_scroll.viewport.rect.size, flexibleSize);
                _scroll.content.pivot = size > viewSize ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
            }
        }

        Vector2 RepositionDefault(int u, int elementPerLine_, Vector2 lineOffset_, Vector2 elementOffset_) {
            return lineOffset_ * (u / elementPerLine_) + elementOffset_ * (u % elementPerLine_);
        }

        Vector2 Repos(int u) {
            //Debug.Log($"pos {u} {pivotOffset} {elementPerLine} {u/elementPerLine}*{lineOffset}+{u%elementPerLine}*{elementOffset}");
            return contentOffset + pivotOffset + Reposition(u, elementPerLine, lineOffset, elementOffset);
        }

        private Entry<T> InitEntry() {
            var entry = new Entry<T>();
            RectTransform element = (RectTransform)GameObject.Instantiate<GameObject>(template.gameObject, _scroll.content, false).transform;
            entry.transform = element;
            entry.item = PrepareT(element);
            return entry;
        }

        public void Refit(int count_) {
            targetCount = count_;
            lineCountViewport = Mathf.CeilToInt(viewportSize/elementSize) + 1;
            int lineCountContent = Mathf.CeilToInt((float)targetCount / elementPerLine);
            //Debug.Log(lineCountContent + " " + lineCountViewport);
            posFixed = lineCountContent < lineCountViewport;
            LineScrollRange = lineCountContent - lineCountViewport;
            RefreshContentSize(lineCountContent * elementSize);
            entryCount = lineCountViewport * elementPerLine;
            entryCount = entryCount < targetCount ? entryCount : targetCount;
            for(int r = 0;r < entry.Count;++r) {
                SwitchT(entry[r].item, false);
            }
            entry.RemoveAll();
            for(int u = 0;u < entryCount;++u) {
                SwitchT(entry.Add().item, true);
            }
        }

        public void Refill() {
            for(int t = 0;t < entryCount;++t) {
                entry[t].transform.anchoredPosition = Repos(indexOffset+t);
                FillT(t, entry[t].item);
            }
        }

        public void Reset() {
            _scroll.StopMovement();
            indexOffset = 0;
            ps = 0;
            pe = entryCount - 1;
            _scroll.content.anchoredPosition = restPos;
            Refill();
        }

        public void Reset(int count_) {
            if(vertical) {
                flexibleSize = Vector2.up;
                fixedSize = new Vector2(_scroll.content.sizeDelta.x, 0);
                restPos = new Vector2(_scroll.content.anchoredPosition.x, 0);
                viewportSize = _scroll.viewport.rect.height;
                xScale = 0;
                yScale = 1;
            }
            else {
                flexibleSize = Vector2.right;
                fixedSize = new Vector2(0, _scroll.content.sizeDelta.y);
                restPos = new Vector2(0, _scroll.content.anchoredPosition.y);
                viewportSize = _scroll.viewport.rect.width;
                xScale = 1;
                yScale = 0;
            }
            Refit(count_);
            Reset();
        }

        public T Ref(int u) {
            return entry[u].item;
        }
    }
}
