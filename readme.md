# ArtemisFgTools

## 立绘合成

```
//你可以通过ArtemisFgTools.exe -h了解基础用法
Usage: tools.exe -c <fgPath> (-s <scriptPath> | -t <luaTablePath>) -o <outputPath>

<fgPath>: fg素材文件夹, 文件夹应该包含了一堆角色名的子文件夹
<scriptPath>: script脚本文件夹, 文件夹应该包含了一堆.ast游戏脚本
<luaTablePath>: 游戏数据表的路径, 部分游戏有提供
<outputPath>: 最后保存的位置
```

### 经过测试的游戏

- セレクトオブリージュ (表: pc\ja\extra\exlist.ipt)

- ハミダシクリエイティブ (表: system\table\exlist.tbl)

- 遥かなるニライカナイ (没找到表)

### 使用方法

1. 提取fg文件夹 (image\fg) 和表文件 (如果有)

2. 根据游戏选择合适方法

    - 有数据表: 

        ```tools.exe -c <fgPath> -t <luaTablePath> -o <outputPath>```

    - 没数据表: 

        1. 读取所有脚本, 根据游戏中的情况合并图像(可能有大量遗漏,所有图像素材会被分类为base和face)

            ```tools.exe -c <fgPath> -s <scriptPath> -o <outputPath>```

        2. 直接根据图像素材名分类并组合(没有遗漏,所有图像素材会被分类为base和face)

            ```tools.exe -c <fgPath> -o <outputPath>```

### 旧版教程

[链接](README1.md)

