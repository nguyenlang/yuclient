   M S B A M L   `   `   `  ���� B  >AutoBuy, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:AutoBuyAutoBuy      AutoBuy.DetailDiaglog   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     'localclr-namespace:AutoBuy       � Title$  DetailDiaglog��$	��300��$	��300��.����  �� Orientation$ Vertical=�$��Center=�.+��� $ 
Horizontal=�$��5 10 0 5q�.+��� $��Name:��$��60���� -    ��Name$ txtName��$	��140������ *Name+�� $ 
Horizontal=�$��5q�.+��� $
��Uid:��$��60���� -   $ txtAsin��$	��140������ *Asin+�� $ 
Horizontal=�$��5q�.+��� $��Status:��$��60���� -   $ 	txtStatus��{�.  $	��140������ *Status+   StatusToText �� 	Converter# [  �� $ 
Horizontal=�$��5q�.+��� $��	Maxprice:��$��60���� -   $ txtMaxPrice��$	��140������ *
MaxPrice+�� $ 
Horizontal=�$��5q�.+��� $��	BuyLimit:��$��60���� -   $ txtBuyLimit��$	��140������ *
BuyLimit+�� $ 
Horizontal=�$��5q�.+��� $��Bought:��$��60���� -   $ txtNoBought��$	��140������ *NumberBought+�� $ 
Horizontal=�$��Right=�$��Bottom=�$��	0 5 -20 0q�.+��� -    ��Name$ Cancel��$��Cancel��$��60��$��23��$
��10 0q��� -   $ Ok��$��Ok��$��60��$��23��