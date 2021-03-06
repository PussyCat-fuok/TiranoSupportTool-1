﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;



namespace ティラノスクリプトサポーター
{
    class FileEncoder
    {
		string Text_Path = "";                               //D＆Dで取得したテキストファイルのパス


		//入出力先フォルダ
		const String DIRECTORY_INPUT   = "input";
		const String DIRECTORY_OUTPUT  = "output";

		//コマンドフォルダ
		const String DIRECTORY_SETTING   = "setting";
		const String SHOWMESSAGE_COMMAND = "showMessage";            //メッセージ開始


		//読み込みクラス
		BackGroundReader bgReader     = new BackGroundReader();
		SoundReader      soundReader  = new SoundReader(); 
		CharacterCommand charaCommand = new CharacterCommand();
		JumpScene        jumpScene    = new JumpScene();
		FadeSetter       fadeSetter   = new FadeSetter();



		string startTemplate;




		public void SetTextPath(string path) { path = Text_Path; }



		public void CreateFiles(ref object sender, ref EventArgs e)
		{
			//読み込みクラス初期化
			BackGroundReader.Initialized();
			SoundReader.Initialized();
			CharacterCommand.Initialized();
			JumpScene.Initialized();
			FadeSetter.Initialized();



			//ファイルのパスを取得
			String path = "test.txt";
			String text = "テストです";
			String[] inputFiles;


			
			if (Text_Path != "")
				inputFiles = Directory.GetFiles(Text_Path);        //D＆Dによるテキスト
			else
				inputFiles = Directory.GetFiles(Path.GetFullPath(DIRECTORY_INPUT), "*");


			//コマンド格納
			//String[] files = Directory.GetFiles(Path.GetFullPath("setting"), "*");
			startTemplate = File.ReadAllText(DIRECTORY_SETTING + "\\" + SHOWMESSAGE_COMMAND + ".txt");



			MarkFile setting = new MarkFile();
			setting.Initialized();



			foreach (String fileName in inputFiles)
			{
				string[] textlines = File.ReadAllLines(fileName);
				outputScenario(ref textlines, ref setting);                      //シーンファイル作成(全て)
				foreach (String test in textlines)
					Console.WriteLine(test);
			}

			Encoding enc = new UTF8Encoding(false);
			File.WriteAllText(path, text, enc);
			Console.WriteLine(Path.GetFullPath(path));


			MessageBox.Show("ファイル書き出し完了", "出力", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}





		//シーンファイルを作成・出力する
		private void outputScenario(ref string[] textlines, ref MarkFile setting)
		{
			int  chapterCnt = 0;                          //チャプターのカウント
			int  sceneCnt   = 1;                          //シーンのカウント
			bool startFlag  = false;
			bool textFlag   = false;                      //テキスト中のフラグ
			var  textList   = new List<string>();
			String newLine  = "[p]";                      //改行



			foreach (String textline in textlines)
			{
				//未入力の場合
				if (textList.Count == 0)
				{
					textList.Add("*start");                   //startラベル挿入
				}

				//キャラクターのメッセージ( # )
				if (textline.Contains(setting.setMarks[(int)SETTING.CHARA]))
				{
					SetEndText(ref textFlag, ref textList);


					charaCommand.TextStart(textline, ref textList);     //背景変更コマンド作成
					textFlag = true;

					newLine = "";
					continue;
				}

				//チャプター切り替え( *** )
				if (textline.Contains(setting.setMarks[(int)SETTING.NEXTCHAPTER]))
				{
					SetEndText(ref textFlag, ref textList);

					//次のチャプター名
					int next = chapterCnt + 1;
					string name = jumpScene.CreateJumpFileName(next, 1);

					
					//最初のみファイル生成を行わない(チャプターカウントを合わせるため)
					if (chapterCnt != 0)
                    {
						//キャラ・音楽消去
						soundReader.StopBGM(ref textList);
						charaCommand.DeleteCharacter(textList);

						outputFile(chapterCnt, sceneCnt, ref textList, name);         //ファイル出力
                    }
						
					chapterCnt++;                                                                             //次のチャプターへ
					sceneCnt  = 1;                                                                            //シーン初期化
					startFlag = false;
				}
				//シーン切り替え( ** )													          
				else if (textline.Contains(setting.setMarks[(int)SETTING.NEXTSCENE]))
				{
					SetEndText(ref textFlag, ref textList);

					//次のシーン名
					int next    = sceneCnt + 1;
					string name = jumpScene.CreateJumpFileName(chapterCnt, next);


					outputFile(chapterCnt, sceneCnt, ref textList, name);       //ファイル出力
					sceneCnt++;                                                                          //シーン初期化
					startFlag = false;
				}
				//背景切り替え( * )													             
				else if (textline.Contains(setting.setMarks[(int)SETTING.BACKGROUND]))
				{
					SetEndText(ref textFlag, ref textList);

					//背景・BGMの変更
					bgReader.CulcTextureCommand(textline, ref textList);    
					soundReader.PlayBGM(textline, ref textList);


					//初回のみの処理
					if (!startFlag)
					{
						fadeSetter.SetFadeOut(ref textList);
						textList.Add(startTemplate);      //開始コマンド挿入
						startFlag = true;
					}
				}
				//通常メッセージ														          
				else
				{
					//空白行以外で改行を入れる
					if (textline != "")
						newLine = "[p]";
					else
						newLine = "";

					textList.Add(textline + newLine);                                            //メッセージ挿入
				}
			}
		}




		//ファイル出力
		private void outputFile(int chapterCnt, int sceneCnt, ref List<string> textList, String nextFile)
		{
			fadeSetter.SetFadeOut(ref textList);      //フェードイン
			textList.Add(nextFile);                   //次のシーンを記述

			String text  = String.Join("\n", textList.ToArray());
			String path  = DIRECTORY_OUTPUT + "\\" + "Chapter" + chapterCnt + "Scene" + sceneCnt + ".ks";      //ファイル命名
			Encoding enc = new UTF8Encoding(false);
			File.WriteAllText(path, text, enc);


			textList = new List<string>();
		}



		private void SetEndText(ref bool textFlag, ref List<string> textList)
		{
			//キャラのテキストの後
			if (textFlag)
            {
				textList.Add("[_tb_end_text]");
				textFlag = false;
            }
		}
	}
}