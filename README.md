---

## 工程介绍   

此工程热更模块基于 HybridCLR + Addressable， 主要展示资源和代码热更的基础工程。  
将核心代码制作为UPM,将 https://github.com/ManoKing/UPMFramework.git#hotfix 添加到  
![Image](https://github.com/ManoKing/UPMFramework/blob/main/ReadMe/add_url.png)  
热更部分即导入项目中  

（1）代码热更基于[HybridCLR跳转](https://github.com/focus-creative-games/HybridCLR)  
（2）资源热更基于[Addressable跳转](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html)     
（3）资源管理基于[Asset Graph跳转](https://docs.unity3d.com/Packages/com.unity.assetgraph@1.7/manual/index.html)   
（4）Jenkins自动打包
  
---

## 如何让项目运行并实现热更  

### HybridCLR编辑器操作
(1)点击执行HybridCLR/Installer打开一个窗体，点击Install等待安装完成  
(2)点击执行HybridCLR/Generate/All, 等待执行完毕    
(3)点击执行HybridCLR/Build/BuildAssetsAndCopyToRes,将Dll生成并拷贝到资源文件夹中  

### 资源管理操作
双击打开AssetGraph/AssetGraph.asset文件，执行右上角的Execute，自动将资源导入Addressables Groups中   

### 游戏入口流程
(1)在Init场景下，对资源进行预下载，关闭了Addressable的自动加载（方便添加白名单测试）    
(2)加载完资源会调用初始化LoadDll,加载热更DLL   
(3)加载完DLL切换热更场景，进入热更模块   

### Addressable 实现本地模拟
利用Addressables Hosting模拟本地加载，或者选择Play Mode Script / Simulate Groups(advanced)

### 其他
Unity 版本使用是2020.3.47f1  


---

## 项目包含一个完整的小游戏实例  
#### 游戏介绍
基于示例跑酷游戏，参考GameFramework和QFramework框架开发  
1,开始选装界面，包含排行榜，商场，任务，设置  
  
![Image](https://github.com/ManoKing/UPMFramework/blob/main/ReadMe/start.png)  
2，跑酷白天主题  
  
![Image](https://github.com/ManoKing/UPMFramework/blob/main/ReadMe/light.png)  
3，跑酷夜晚主题  
  
![Image](https://github.com/ManoKing/UPMFramework/blob/main/ReadMe/night.png)  


---

## FAQ

### 问题
(1)CDN服务器缓存问题，不能及时获取到hash, json   
