using Newtonsoft.Json.Linq;
using DV.JObjectExtstensions;
using TMPro;
using UnityEngine;

namespace ConductorBoard
{
    public class ConductorBoardData : MonoBehaviour
    {
        public TextMeshPro header;
        public TextMeshPro body;
        public TextMeshPro train;
        private ItemSaveData _itemSaveData;
        public string Header { get; set; }
        public string Body{ get; set; }
        public string Train { get; set; }
        public void Awake()
        {
            _itemSaveData = this.gameObject.GetComponent<ItemSaveData>();
            _itemSaveData.ItemSaveDataRequested += _itemSaveData_ItemSaveDataRequested;
            _itemSaveData.AfterItemSaveDataLoaded += AfterSaveDataLoad;
        }

        private void AfterSaveDataLoad(JObject data)
        {
            if (data != null)
            {
                JObject boardData = data.GetJObject("boardData");
                if (boardData != null)
                {
                    Header = boardData.GetString("header");
                    Body = boardData.GetString("body");
                    Train = boardData.GetString("train");
                }
            }
            header.gameObject.SetActive(false);
            body.gameObject.SetActive(false);
            train.gameObject.SetActive(false);
            header.gameObject.SetActive(true);
            body.gameObject.SetActive(true);
            train.gameObject.SetActive(true);
            WriteTexts();
        }

        public void Start()
        {
            WriteTexts();
        }

        public void OnEnable()
        {
            WriteTexts();
        }

        private void WriteTexts()
        {
            header.text = Header;
            header.SetText(Header);
            header.SetAllDirty();
            body.text = Body;
            header.SetAllDirty();
            train.text = Train;
            header.SetAllDirty();
        }

        private JObject _itemSaveData_ItemSaveDataRequested(JObject data)
        {
            JObject boardData = new JObject();
            boardData.SetString("header", Header);
            boardData.SetString("body", Body);
            boardData.SetString("train", Train);
            data.SetJObject("boardData", boardData);
            return data;
        }
    }
}
