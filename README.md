#### 使用方式：

压缩：

- 命令行调用：`MiniBinaryXml.exe -c "源文件路径" "目标文件路径"`
- C#调用: `new MiniBinaryXmlWriter().Write(xElement, fileStream);`

解压缩

- 命令行调用：`MiniBinaryXml.exe -d "源文件路径" "目标文件路径"`
  
- C#调用: `XElement xElement = new MiniBinaryXmlReader().ReadAsXml(fileStream);`
