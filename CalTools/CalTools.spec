# -*- mode: python -*-

block_cipher = None


a = Analysis(['CalTools.py'],
             pathex=['C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools'],
             binaries=[],
             datas=[('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\calendar.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\CalToolsIcon.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\CalToolsIcon.ico','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\edit.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\save.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\folder.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\report.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\delete.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\CalTools\\CalTools\\images\\move.png','images')
			 ],
             hiddenimports=[],
             hookspath=[],
             runtime_hooks=[],
             excludes=['matplotlib'],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher)
pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          a.binaries,
          a.zipfiles,
          a.datas,
          name='CalTools',
          debug=False,
          strip=False,
          upx=True,
          runtime_tmpdir=None,
          console=False , icon='CalToolsIcon.ico')
