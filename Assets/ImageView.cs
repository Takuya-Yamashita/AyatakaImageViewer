using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class ImageView : MonoBehaviour {

	[SerializeField] Image Image = null;
	[SerializeField] Dropdown JsonDropDown = null;
	[SerializeField] Dropdown ImageTagDropDown = null;

	// AyatakaのJsonフォーマットに基づいたシリアライズクラス
	[Serializable]
	class JsonBase {
		public JsonData[] jsonData = null;
	}

	[Serializable]
	class JsonData {
		public string nameKey = "";
		public string atlas = "";
		public string fileName = "";
		public int x = 0;
		public int y = 0;
		public int w = 0;
		public int h = 0;
	}

	// Atlas情報
	// 描画に必要な情報だけピックアップ
	class AtlasData {
		public string Tag = "";
		public int X = 0;
		public int Y = 0;
		public int Width = 0;
		public int Height = 0;
	}

	// Jsonリストを選択した情報。関数をまたぐのでグローバルで実装
	JsonBase JsonBaseData = null;

	private string JsonFileDirectoryPath = "";
	private string AtlasFileDirectoryPath = "";

	// Use this for initialization
	void Start () {
#if UNITY_EDITOR
		// パスの設定
		JsonFileDirectoryPath = "TestDirectory";
		AtlasFileDirectoryPath = "TestDirectory";
#else
		// パスの設定
		JsonFileDirectoryPath = string.Format(
			"..{0}..{1}client{2}assets{3}ui-data",
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar
		);

		AtlasFileDirectoryPath = string.Format(
			"..{0}..{1}client{2}assets{3}",
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar
		);
#endif
		// 指定ディレクトリから、jsonファイル一覧を取得
		string[] files = System.IO.Directory.GetFiles(JsonFileDirectoryPath, "*.json", System.IO.SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++) {
			files[i] = files[i].Replace(JsonFileDirectoryPath + System.IO.Path.DirectorySeparatorChar, "");
		}
		JsonDropDown.ClearOptions();
		List<string> fileList = new List<string>(files);
		fileList.Insert(0, "NONE");
		JsonDropDown.AddOptions(fileList);
	}
	
	// Update is called once per frame
	void Update () {
	}

	// UnityのResourceではない場所からの、画像ファイル読み込み
	// https://qiita.com/ele_enji/items/5faf1b48c8db2e08f393
	private Texture2D LoadTexture2DFromFile(string path) {
		Texture2D texture = null;
		if (File.Exists(path)) {
			//byte取得
			FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader bin = new BinaryReader(fileStream);
			byte[] readBinary = bin.ReadBytes((int)bin.BaseStream.Length);
			bin.Close();
			fileStream.Dispose();
			fileStream = null;
			if (readBinary != null)	{
				//横サイズ
				int pos = 16;
				int width = 0;
				for (int i = 0; i < 4; i++)	{
					width = width * 256 + readBinary[pos++];
				}
				//縦サイズ
				int height = 0;
				for (int i = 0; i < 4; i++)	{
					height = height * 256 + readBinary[pos++];
				}
				//byteからTexture2D作成
				texture = new Texture2D(width, height);
				texture.LoadImage(readBinary);
			}
			readBinary = null;
		}
		return texture;
	}

	// テクスチャ情報を渡して、オフセットとサイズを渡して、Spriteの作成
	private Sprite CreateSpriteFromTexture2D(Texture2D texture, int x, int y, int width, int height) {
		Sprite sprite = null;
		if (texture) {
			//Texture2DからSprite作成
			sprite = Sprite.Create(texture, new UnityEngine.Rect(x, y, width, height), Vector2.zero);
		}
		return sprite;
	}

	// jsonファイルの選択
	public void ChangeJsonDropdown(int index) {
		string path = JsonDropDown.captionText.text;

		if (path == "NONE") {
			return;
		}

		FileStream fileStream = new FileStream(JsonFileDirectoryPath + System.IO.Path.DirectorySeparatorChar + path, FileMode.Open, FileAccess.Read);
		BinaryReader bin = new BinaryReader(fileStream);
		byte[] readBinary = bin.ReadBytes((int)bin.BaseStream.Length);
		bin.Close();
		fileStream.Dispose();
		fileStream = null;
		string text = System.Text.Encoding.UTF8.GetString(readBinary);
		readBinary = null;

		// AyatakaのJsonファイルフォーマットは、そのままJsonUtilityで読めるフォーマットにはなっていない（はず）なので、
		// データ名タグを追加
		text = "{\"jsonData\": " + text + "}";

		JsonBaseData = JsonUtility.FromJson<JsonBase>(text);
		List<string> atlasList = new List<string>();
		for (int i = 0; i < JsonBaseData.jsonData.Length; i++) {
			if (string.IsNullOrEmpty(JsonBaseData.jsonData[i].atlas) == false) {
				// imagesとbgは、それその物を参照する物ではないらしいので、弾く
				if (JsonBaseData.jsonData[i].atlas.IndexOf("ui-atlas" + "/" + "images") == -1) {
					if (JsonBaseData.jsonData[i].atlas.IndexOf("ui-atlas" + "/" + "bg") == -1) {
						atlasList.Add(JsonBaseData.jsonData[i].nameKey);
					}
				}
			}
		}
		ImageTagDropDown.ClearOptions();
		atlasList.Insert(0, "NONE");
		ImageTagDropDown.AddOptions(atlasList);
	}

	// イメージタグの選択
	public void ChangeImageTagDropdown(int index) {
		string nameKey = ImageTagDropDown.captionText.text;
		if (nameKey == "NONE") {
			return;
		}

		string atlasName = "";
		string fileName = "";
		for (int i = 0; i < JsonBaseData.jsonData.Length; i++) {
			if (JsonBaseData.jsonData[i].nameKey == nameKey) {
				atlasName = JsonBaseData.jsonData[i].atlas;
				fileName = JsonBaseData.jsonData[i].fileName;
				break;
			}
		}

		FileStream fileStream = new FileStream(AtlasFileDirectoryPath + System.IO.Path.DirectorySeparatorChar + atlasName + ".atlas", FileMode.Open, FileAccess.Read);
		BinaryReader bin = new BinaryReader(fileStream);
		byte[] readBinary = bin.ReadBytes((int)bin.BaseStream.Length);
		bin.Close();
		fileStream.Dispose();
		fileStream = null;
		string text = System.Text.Encoding.UTF8.GetString(readBinary);
		readBinary = null;

		string[] splitList = text.Split("\n"[0]);
		AtlasData atlasData = null;
		bool isSetXY = false;
		bool isSetSize = false;
		// 6行目まで読み飛ばし
		for (int i = 6; i < splitList.Length; i++) {
			if (isSetXY && isSetSize) {
				break;
			}
			if (splitList[i].StartsWith(" ") == false) {
				if (splitList[i].IndexOf(fileName) == -1) {
					continue;
				} else {
					atlasData = new AtlasData();
				}
			} else {
				if (atlasData == null) {
					continue;
				}
				string line = splitList[i];
				line = line.Replace(" ", "");
				line = line.Replace("\n", "");
				string[] lineList = line.Split(":"[0]);
				// splitは対応方法がわからなかったので、いったん対応しない
				if (lineList[0] == "xy") {
					string[] posList = lineList[1].Split(","[0]);
					atlasData.X = int.Parse(posList[0]);
					atlasData.Y = int.Parse(posList[1]);
					isSetXY = true;
				} else if (lineList[0] == "size") {
					string[] sizeList = lineList[1].Split(","[0]);
					atlasData.Width = int.Parse(sizeList[0]);
					atlasData.Height = int.Parse(sizeList[1]);
					isSetSize = true;
				}
			}
		}

		Texture2D tex = LoadTexture2DFromFile(AtlasFileDirectoryPath + System.IO.Path.DirectorySeparatorChar + atlasName + ".png");
		// JsonやAtlasの設定は左上原点だが、どうやらUnityは左下原点なので、わかりづらい調整計算をしている
		Sprite sprite = CreateSpriteFromTexture2D(tex, atlasData.X, tex.height - atlasData.Y - atlasData.Height, atlasData.Width, atlasData.Height);
		Image.sprite = sprite;
		Image.SetNativeSize();
	}
}
