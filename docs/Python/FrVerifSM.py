#Boa:Frame:Frame1

import wx
import wx.lib.masked.timectrl
import wx.lib.masked.textctrl
import wx.grid
from dbfpy.dbf import Dbf
import math

def create(parent):
    return Frame1(parent)

[wxID_FRAME1, wxID_FRAME1BTPROCESAR, wxID_FRAME1BUTTON1, wxID_FRAME1BUTTON2, 
 wxID_FRAME1GAUGE1, wxID_FRAME1LISTCTRL1, wxID_FRAME1LISTCTRL2, 
 wxID_FRAME1STATICTEXT1, wxID_FRAME1TEXTCTRL1, wxID_FRAME1TEXTCTRL2, 
] = [wx.NewId() for _init_ctrls in range(10)]

class Frame1(wx.Frame):
    def _init_coll_listCtrl1_Columns(self, parent):
        # generated method, don't edit

        parent.InsertColumn(col=0, format=wx.LIST_FORMAT_LEFT, heading='Lance',
              width=-1)
        parent.InsertColumn(col=1, format=wx.LIST_FORMAT_LEFT,
              heading='Nro. Ejemplar', width=-1)
        parent.InsertColumn(col=2, format=wx.LIST_FORMAT_LEFT, heading='Largo',
              width=-1)
        parent.InsertColumn(col=3, format=wx.LIST_FORMAT_LEFT, heading='Sexo',
              width=-1)
        parent.InsertColumn(col=4, format=wx.LIST_FORMAT_LEFT,
              heading='Estad\xedo', width=-1)

    def _init_coll_listCtrl2_Columns(self, parent):
        # generated method, don't edit

        parent.InsertColumn(col=0, format=wx.LIST_FORMAT_LEFT, heading='Lance',
              width=-1)
        parent.InsertColumn(col=1, format=wx.LIST_FORMAT_LEFT, heading='Largo',
              width=-1)
        parent.InsertColumn(col=2, format=wx.LIST_FORMAT_LEFT, heading='Machos',
              width=-1)
        parent.InsertColumn(col=3, format=wx.LIST_FORMAT_LEFT,
              heading='Hembras', width=-1)
        parent.InsertColumn(col=4, format=wx.LIST_FORMAT_LEFT, heading='Indet.',
              width=-1)
        parent.InsertColumn(col=5, format=wx.LIST_FORMAT_LEFT,
              heading='Machos Esp.', width=-1)
        parent.InsertColumn(col=6, format=wx.LIST_FORMAT_LEFT,
              heading='Hembras Esp.', width=-1)
        parent.InsertColumn(col=7, format=wx.LIST_FORMAT_LEFT,
              heading='Indet. Esp.', width=-1)

    def _init_ctrls(self, prnt):
        # generated method, don't edit
        wx.Frame.__init__(self, id=wxID_FRAME1, name='', parent=prnt,
              pos=wx.Point(300, 99), size=wx.Size(685, 441),
              style=wx.DEFAULT_FRAME_STYLE,
              title='Validaci\xf3n de Submuestras')
        self.SetClientSize(wx.Size(677, 407))

        self.textCtrl1 = wx.TextCtrl(id=wxID_FRAME1TEXTCTRL1, name='textCtrl1',
              parent=self, pos=wx.Point(16, 25), size=wx.Size(280, 21), style=0,
              value='D:\\Usuarios\\Daniel\\Documentos\\Inidep\\OBS\\M_07315.DBF')

        self.button1 = wx.Button(id=wxID_FRAME1BUTTON1, label='...',
              name='button1', parent=self, pos=wx.Point(304, 25),
              size=wx.Size(24, 23), style=0)
        self.button1.SetToolTipString('Seleccionar archivo de muestras')
        self.button1.Bind(wx.EVT_BUTTON, self.OnButton1Button,
              id=wxID_FRAME1BUTTON1)

        self.textCtrl2 = wx.TextCtrl(id=wxID_FRAME1TEXTCTRL2, name='textCtrl2',
              parent=self, pos=wx.Point(346, 25), size=wx.Size(280, 21),
              style=0,
              value='D:\\Usuarios\\Daniel\\Documentos\\Inidep\\OBS\\S_07315.DBF')

        self.button2 = wx.Button(id=wxID_FRAME1BUTTON2, label='...',
              name='button2', parent=self, pos=wx.Point(634, 25),
              size=wx.Size(24, 23), style=0)
        self.button2.SetToolTipString('Seleccionar archivo de muestras')
        self.button2.Bind(wx.EVT_BUTTON, self.OnButton2Button,
              id=wxID_FRAME1BUTTON2)

        self.staticText1 = wx.StaticText(id=wxID_FRAME1STATICTEXT1,
              label='Marea:', name='staticText1', parent=self, pos=wx.Point(16,
              8), size=wx.Size(46, 16), style=0)
        self.staticText1.SetFont(wx.Font(10, wx.SWISS, wx.NORMAL, wx.BOLD,
              False, 'Tahoma'))

        self.listCtrl1 = wx.ListCtrl(id=wxID_FRAME1LISTCTRL1, name='listCtrl1',
              parent=self, pos=wx.Point(16, 200), size=wx.Size(312, 200),
              style=wx.LC_REPORT)
        self._init_coll_listCtrl1_Columns(self.listCtrl1)

        self.listCtrl2 = wx.ListCtrl(id=wxID_FRAME1LISTCTRL2, name='listCtrl2',
              parent=self, pos=wx.Point(16, 56), size=wx.Size(640, 128),
              style=wx.LC_REPORT)
        self._init_coll_listCtrl2_Columns(self.listCtrl2)

        self.btProcesar = wx.Button(id=wxID_FRAME1BTPROCESAR, label='Procesar',
              name='btProcesar', parent=self, pos=wx.Point(576, 192),
              size=wx.Size(75, 23), style=0)
        self.btProcesar.Bind(wx.EVT_BUTTON, self.OnButton3Button,
              id=wxID_FRAME1BTPROCESAR)

        self.gauge1 = wx.Gauge(id=wxID_FRAME1GAUGE1, name='gauge1', parent=self,
              pos=wx.Point(344, 224), range=100, size=wx.Size(308, 16),
              style=wx.GA_HORIZONTAL)
        self.gauge1.Show(False)
        self.gauge1.SetValue(0)
        self.gauge1.SetLabel('')
        self.gauge1.SetToolTipString('gauge1')

    def __init__(self, parent):
        self._init_ctrls(parent)

    def OnButton1Button(self, event):
        dlg = wx.FileDialog(self, "Seleccione DBF de Muestras", ".", "", "M_*.dbf", wx.OPEN)
        try:
            if dlg.ShowModal() == wx.ID_OK:
                filename = dlg.GetPath()
                # Your code
                self.textCtrl1.Value =filename
                dbm=Dbf(filename,True)
                self.staticText1.Label = 'Marea: '+str(dbm[1]['MAREA'])
        finally:
            dlg.Destroy()
        event.Skip()

    def OnButton2Button(self, event):
        dlg = wx.FileDialog(self, "Seleccione DBF de Submuestras", ".", "", "S_*.dbf", wx.OPEN)
        try:
            if dlg.ShowModal() == wx.ID_OK:
                filename = dlg.GetPath()
                # Your code
                self.textCtrl2.Value =filename
                dbs=Dbf(filename,True)
        finally:
            dlg.Destroy()
        event.Skip()

    def OnButton3Button(self, event):
        self.gauge1.Show()
        dbm=Dbf(self.textCtrl1.Value,True)
        dbs=Dbf(self.textCtrl2.Value,True)
        self.gauge1.Range=dbm.recordCount
        self.gauge1.Value=0;
        # Se copia la tabla de submuestras a un array para procesar mas rapido
        SM=[]
        ERRORES=[]
        for r in dbs:
            SM.append(r)
        self.listCtrl2.DeleteAllItems()
        for r in dbm:
            self.gauge1.Value=r.index+1
            lance=r['LANCE']
            especie=r['ESPECIE']
            #SML=[]
            #for r1 in SM:
            #    if r1['LANCE']==lance:
            #        SML.append(r1)
            #Se reemplaza lo anterior por una 'List comprehension'
            SML=[r1 for r1 in SM if (r1['LANCE']==lance and r1['ESPECIE']==especie)] #Esto filtra la lista SM
            if len(SML)==0:
                continue
            for i in range(1,91):
                str_dato=str(r['TALLA_'+str(i)])
                #print str_dato, len(str_dato)
                if str_dato=='' or len(str_dato)<>14:
                    continue
                talla=int(str_dato[0:2])
                #print i,str_dato,'M: ',str_dato[2:5]
                machos=str_dato[2:5]
                hembras=str_dato[5:8]
                indet=str_dato[8:11]
                machos_esp=0
                hembras_esp=0
                indet_esp=0
                machos_enc=0
                hembras_enc=0
                indet_enc=0

                if machos>0:
                    machos_esp=math.trunc((int(machos)-1)/5)+1
                if hembras>0:
                    hembras_esp=math.trunc((int(hembras)-1)/5)+1
                if indet>0:
                    indet_esp=math.trunc((int(indet)-1)/5)+1

                for r2 in SML:
                    if r2['LARGO_TOT']<>talla:
                        continue
                    #print r2['LARGO_TOT'],talla,r2['SEXO']
                    if r2['SEXO']==1:
                        machos_enc=machos_enc+1
                    if r2['SEXO']==2:
                        hembras_enc=hembras_enc+1
                    if r2['SEXO']==3:
                        indet_enc=indet_enc+1
                #print machos_enc, hembras_enc, indet_enc
                if machos_enc<>machos_esp or hembras_enc<>hembras_esp or indet_enc<>indet_esp:
                    ERRORES.append([lance,talla,machos_enc,hembras_enc,indet_enc,machos_esp,hembras_esp,indet_esp])
        #print 'ERRORES:'
        #for i in range(0,len(ERRORES)):
            #print ERRORES[i]
        #print 'FIN'
        if len(ERRORES)==0:
            mensaje = "No se han encontrado errores en ninguna submuestra "+'\n'
            msg = wx.MessageDialog(self, mensaje, 'Informacion',wx.OK | wx.ICON_INFORMATION)
            msg.ShowModal()
            msg.Destroy()
        else:
            for x in range(len(ERRORES)):
                item=ERRORES[x]
                self.listCtrl2.InsertStringItem(x,str(item[0]))
                self.listCtrl2.SetStringItem(x,1,str(item[1]))
                self.listCtrl2.SetStringItem(x,2,str(item[2]))
                self.listCtrl2.SetStringItem(x,3,str(item[3]))
                self.listCtrl2.SetStringItem(x,4,str(item[4]))
                self.listCtrl2.SetStringItem(x,5,str(item[5]))
                self.listCtrl2.SetStringItem(x,6,str(item[6]))
                self.listCtrl2.SetStringItem(x,7,str(item[7]))
            self.listCtrl2.SetColumnWidth(7, wx.LIST_AUTOSIZE)
            mensaje = "Se han detectado algunas inconsistencias o errores "+'\n'
            msg = wx.MessageDialog(self, mensaje, 'Atencion',wx.OK | wx.ICON_WARNING)
            msg.ShowModal()
            msg.Destroy()
        self.gauge1.Hide()
        event.Skip()
