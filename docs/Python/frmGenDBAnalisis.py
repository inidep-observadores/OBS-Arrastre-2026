#Boa:Frame:Frame1
import wx
from wx.lib.anchors import LayoutAnchors
import wx.lib.filebrowsebutton
from dbfpy.dbf import Dbf
import sqlite3 as sq3
import glob
import fpformat as fpf

#Variables globales
list_mareas={}

def create(parent):
    return Frame1(parent)

[wxID_FRAME1, wxID_FRAME1BTCERRAR, wxID_FRAME1BTEXAMINAR, 
 wxID_FRAME1BTPROCESAR, wxID_FRAME1CHMAREA, wxID_FRAME1GAUGE1, 
 wxID_FRAME1LCRESULTADO, wxID_FRAME1LCRESULTFECHA, wxID_FRAME1RBTOTFECHA, 
 wxID_FRAME1RBTOTGEN, wxID_FRAME1STATICTEXT1, wxID_FRAME1STATICTEXT2, 
 wxID_FRAME1STATICTEXT3, wxID_FRAME1TCCARPETA, 
] = [wx.NewId() for _init_ctrls in range(14)]

class Frame1(wx.Frame):
    def _init_coll_lcResultFecha_Columns(self, parent):
        # generated method, don't edit

        parent.InsertColumn(col=0, format=wx.LIST_FORMAT_LEFT, heading='Fecha',
              width=80)
        parent.InsertColumn(col=1, format=wx.LIST_FORMAT_LEFT,
              heading='Especie', width=80)
        parent.InsertColumn(col=2, format=wx.LIST_FORMAT_RIGHT,
              heading='Aprovechado', width=85)
        parent.InsertColumn(col=3, format=wx.LIST_FORMAT_RIGHT,
              heading='Capt.Reconst.', width=85)
        parent.InsertColumn(col=4, format=wx.LIST_FORMAT_RIGHT, heading='% dif',
              width=60)
        parent.InsertColumn(col=5, format=wx.LIST_FORMAT_RIGHT,
              heading='Producido', width=85)

    def _init_coll_lcResultado_Columns(self, parent):
        # generated method, don't edit

        parent.InsertColumn(col=0, format=wx.LIST_FORMAT_LEFT, heading='Marea',
              width=45)
        parent.InsertColumn(col=1, format=wx.LIST_FORMAT_LEFT,
              heading='Especie', width=100)
        parent.InsertColumn(col=2, format=wx.LIST_FORMAT_RIGHT,
              heading='Aprovechado', width=90)
        parent.InsertColumn(col=3, format=wx.LIST_FORMAT_RIGHT,
              heading='Capt.Reconst.', width=90)
        parent.InsertColumn(col=4, format=wx.LIST_FORMAT_RIGHT, heading='% dif',
              width=50)
        parent.InsertColumn(col=5, format=wx.LIST_FORMAT_RIGHT,
              heading='Producido', width=90)

    def _init_ctrls(self, prnt):
        # generated method, don't edit
        wx.Frame.__init__(self, id=wxID_FRAME1, name='', parent=prnt,
              pos=wx.Point(611, 169), size=wx.Size(514, 510),
              style=wx.DEFAULT_FRAME_STYLE, title='Datos de capturas')
        self.SetClientSize(wx.Size(498, 472))
        self.Center(wx.BOTH)
        self.SetMaxSize(wx.Size(-1, -1))
        self.SetMinSize(wx.Size(464, 190))
        self.SetToolTipString('Frame1')
        self.Bind(wx.EVT_ACTIVATE, self.OnFrame1Activate)

        self.staticText3 = wx.StaticText(id=wxID_FRAME1STATICTEXT3,
              label='Carpeta de datos', name='staticText3', parent=self,
              pos=wx.Point(8, 24), size=wx.Size(85, 13), style=0)

        self.staticText1 = wx.StaticText(id=wxID_FRAME1STATICTEXT1,
              label='Generaci\xf3n de datos para an\xe1lisis de capturas',
              name='staticText1', parent=self, pos=wx.Point(0, 0),
              size=wx.Size(441, 23), style=0)
        self.staticText1.SetToolTipString('staticText1')
        self.staticText1.SetFont(wx.Font(14, wx.SWISS, wx.ITALIC, wx.BOLD,
              False, 'Tahoma'))
        self.staticText1.SetForegroundColour(wx.Colour(0, 0, 0))

        self.btExaminar = wx.Button(id=wxID_FRAME1BTEXAMINAR, label='Examinar',
              name='btExaminar', parent=self, pos=wx.Point(408, 40),
              size=wx.Size(75, 23), style=0)
        self.btExaminar.Bind(wx.EVT_BUTTON, self.OnBtExaminarButton,
              id=wxID_FRAME1BTEXAMINAR)

        self.tcCarpeta = wx.TextCtrl(id=wxID_FRAME1TCCARPETA, name='tcCarpeta',
              parent=self, pos=wx.Point(8, 40), size=wx.Size(392, 21), style=0,
              value='D:\\Usuarios\\Daniel\\Documentos\\Inidep\\OBS')
        self.tcCarpeta.SetEditable(False)

        self.chMarea = wx.Choice(choices=[], id=wxID_FRAME1CHMAREA,
              name='chMarea', parent=self, pos=wx.Point(48, 71),
              size=wx.Size(130, 21), style=0)
        self.chMarea.SetStringSelection('')
        self.chMarea.SetLabel('')
        self.chMarea.SetHelpText('')
        self.chMarea.SetToolTipString('Seleccione marea a procesar')
        self.chMarea.SetConstraints(LayoutAnchors(self.chMarea, True, False,
              False, True))

        self.btProcesar = wx.Button(id=wxID_FRAME1BTPROCESAR, label='Procesar',
              name='btProcesar', parent=self, pos=wx.Point(408, 71),
              size=wx.Size(75, 23), style=0)
        self.btProcesar.SetDefault()
        self.btProcesar.Enable(True)
        self.btProcesar.SetConstraints(LayoutAnchors(self.btProcesar, False,
              False, True, True))
        self.btProcesar.Bind(wx.EVT_BUTTON, self.OnBtProcesarButton,
              id=wxID_FRAME1BTPROCESAR)

        self.btCerrar = wx.Button(id=wxID_FRAME1BTCERRAR, label='Cerrar',
              name='btCerrar', parent=self, pos=wx.Point(328, 71),
              size=wx.Size(75, 23), style=0)
        self.btCerrar.SetConstraints(LayoutAnchors(self.btCerrar, False, False,
              True, True))
        self.btCerrar.Bind(wx.EVT_BUTTON, self.OnBtCerrarButton,
              id=wxID_FRAME1BTCERRAR)

        self.staticText2 = wx.StaticText(id=wxID_FRAME1STATICTEXT2,
              label='Marea:', name='staticText2', parent=self, pos=wx.Point(8,
              71), size=wx.Size(35, 13), style=0)

        self.gauge1 = wx.Gauge(id=wxID_FRAME1GAUGE1, name='gauge1', parent=self,
              pos=wx.Point(8, 445), range=100, size=wx.Size(480, 16),
              style=wx.GA_HORIZONTAL)
        self.gauge1.Show(False)

        self.lcResultado = wx.ListCtrl(id=wxID_FRAME1LCRESULTADO,
              name='lcResultado', parent=self, pos=wx.Point(8, 128),
              size=wx.Size(480, 312), style=wx.LC_REPORT)
        self._init_coll_lcResultado_Columns(self.lcResultado)

        self.lcResultFecha = wx.ListCtrl(id=wxID_FRAME1LCRESULTFECHA,
              name='lcResultFecha', parent=self, pos=wx.Point(8, 128),
              size=wx.Size(480, 312), style=wx.LC_REPORT)
        self.lcResultFecha.Show(False)
        self._init_coll_lcResultFecha_Columns(self.lcResultFecha)

        self.rbTotGen = wx.RadioButton(id=wxID_FRAME1RBTOTGEN,
              label='Totales generales', name='rbTotGen', parent=self,
              pos=wx.Point(16, 104), size=wx.Size(120, 13), style=0)
        self.rbTotGen.SetValue(True)
        self.rbTotGen.SetToolTipString('rbTotGen')
        self.rbTotGen.Bind(wx.EVT_RADIOBUTTON, self.OnRbTotGenRadiobutton,
              id=wxID_FRAME1RBTOTGEN)

        self.rbTotFecha = wx.RadioButton(id=wxID_FRAME1RBTOTFECHA,
              label='Totales por fecha', name='rbTotFecha', parent=self,
              pos=wx.Point(152, 104), size=wx.Size(112, 13), style=0)
        self.rbTotFecha.SetValue(True)
        self.rbTotFecha.Bind(wx.EVT_RADIOBUTTON, self.OnRbTotFechaRadiobutton,
              id=wxID_FRAME1RBTOTFECHA)

    def __init__(self, parent):
        self._init_ctrls(parent)
        path=self.tcCarpeta.Value
        if path <> '':
            self.chMarea.Items=[]
            list_mareas.clear()
            dbfs=glob.glob(path+'\\C_*.dbf')
            for arch in dbfs:
                tmpdb=Dbf(arch)
                for r in tmpdb:
                    str_marea=str(r['FECHA'].year) +' - ' + ('0'+str(r['MAREA']))[-3:]
                    if not (str_marea in list_mareas): 
                        list_mareas[str_marea]=arch
            items=list_mareas.keys()
            items.sort()
            self.chMarea.AppendItems(items)

    def OnBtCerrarButton(self, event):
        self.Close()
        event.Skip()

    def OnBtProcesarButton(self, event):
        db = './DB/dbAnalisis.s3db'
        #cargo lista de especies en un diccionario
        dbe=Dbf(self.tcCarpeta.Value+'\ESPECIE1.dbf')
        especies={}
        for r in dbe:
            if r['NOMVULCAS'].strip('\xf7')<>'':
                cod_esp=str(r['CODINIDEP'])
                if cod_esp not in especies:
                    especies[cod_esp]=r['NOMVULCAS'].strip('\xf7')
			
        con = sq3.connect(db)
        cur = con.cursor()
        cur.execute('delete from Lances')
        cur.execute('delete from Produccion')
        con.commit()
        
        dbl=Dbf(list_mareas[self.chMarea.StringSelection])
        try:
            m=self.chMarea.StringSelection
            cod_marea=m[7:10]+m[2:4]
            dbf_prod=self.tcCarpeta.Value+'\P_'+cod_marea+'.dbf'
            dbp=Dbf(dbf_prod)
                
        except:
            dlg = wx.MessageDialog(self, 'No se encontro la base de datos de produccion:\n\n'+dbf_prod, 'Error', wx.OK | wx.ICON_ERROR)
            try:
                result = dlg.ShowModal()
            finally:
                dlg.Destroy()
        
        for r in dbp:
            try:
                clave = (r['MAREA'],r['FECHA'],r['ESPECIE'],r['PRODUCTO'].decode('latin-1'))
                cur.execute('select * from Produccion where Marea = ? and Fecha = ? and Especie = ? and Producto = ?', clave)
            except:
                print r['PRODUCTO'].decode('latin-1')
            rs = cur.fetchone()
            if not rs:
                nuevos_datos=(r['MAREA'],r['FECHA'],r['ESPECIE'],r['PRODUCTO'].decode('latin-1'),r['FACTOR'],r['KILOS'])
                cur.execute('INSERT INTO Produccion (Marea,Fecha,Especie,Producto,Factor,Producido) VALUES(?,?,?,?,?,?)',nuevos_datos)
            else:
                nuevos_datos=(r['KILOS'],r['MAREA'],r['FECHA'],r['ESPECIE'],r['PRODUCTO'].decode('latin-1'))
                cur.execute('UPDATE Produccion SET Producido=Producido+? where Marea = ? and Fecha = ? and Especie = ? and Producto = ?',nuevos_datos)
        con.commit()
        self.gauge1.Range=len(db)
        self.gauge1.Show()
        for r in dbl:
            self.gauge1.Value=r.index+1
            for i in range(1,26):
                cod=str(r['ESPECIE_'+str(i)])[:11]
                if cod > 0 and cod in especies:
                    clave = (r['MAREA'],r['LANCE'],cod)
                    captura=r['KG_'+str(i)]
                    descarte=captura*r['DESCAR_'+str(i)]/100
                    cur.execute('select * from Lances where Marea = ? and Lance = ? and Especie = ?', clave)
                    rs = cur.fetchone()
                    if not rs:
                        nuevos_datos=(r['MAREA'],r['LANCE'],r['FECHA'],cod, especies[cod],captura,descarte)
                        cur.execute('INSERT INTO Lances (Marea,Lance,Fecha,Especie,DscEspecie,Captura,Descarte) VALUES(?,?,?,?,?,?,?)',nuevos_datos)
        con.commit()
        self.lcResultado.DeleteAllItems()
        cur.execute('select * from v_CompCaptProd')
        rs=cur.fetchall()
        for x in range(len(rs)):
            r=rs[x]
            self.lcResultado.InsertStringItem(x,str(r[0]))
            self.lcResultado.SetStringItem(x,1,str(r[1]))
            self.lcResultado.SetStringItem(x,2,fpf.fix(r[2],2))
            self.lcResultado.SetStringItem(x,3,fpf.fix(r[3],2))
            self.lcResultado.SetStringItem(x,4,fpf.fix((r[3]-r[2])*100/r[2],2))
            self.lcResultado.SetStringItem(x,5,fpf.fix(r[4],2))
            
        self.lcResultFecha.DeleteAllItems()
        cur.execute('select * from v_CompCaptProdFecha')
        rs=cur.fetchall()
        for x in range(len(rs)):
            r=rs[x]
            self.lcResultFecha.InsertStringItem(x,str(r[1]))
            self.lcResultFecha.SetStringItem(x,1,str(r[2]))
            self.lcResultFecha.SetStringItem(x,2,fpf.fix(r[3],2))
            self.lcResultFecha.SetStringItem(x,3,fpf.fix(r[4],2))
            self.lcResultFecha.SetStringItem(x,4,fpf.fix((r[4]-r[3])*100/r[3],2))
            self.lcResultFecha.SetStringItem(x,5,fpf.fix(r[5],2))
            
        self.gauge1.Hide()
        event.Skip()

    def OnBtExaminarButton(self, event):
        "Permite seleccionar la carpeta de datos y llena la lista de seleccion de mareas."
        dlg = wx.DirDialog(self)
        try:
            dlg.SetPath(self.tcCarpeta.Value)
            if dlg.ShowModal() == wx.ID_OK:
                path = dlg.GetPath()
                self.tcCarpeta.Value=path
                self.chMarea.Items=[]
                list_mareas.clear()
                dbfs=glob.glob(path+'\\C_*.dbf')
                for arch in dbfs:
                    tmpdb=Dbf(arch)
                    for r in tmpdb:
                        str_marea=str(r['FECHA'].year) +' - ' + ('0'+str(r['MAREA']))[-3:]
                        if not (str_marea in list_mareas): 
                            list_mareas[str_marea]=arch
                items=list_mareas.keys()
                items.sort()
                self.chMarea.AppendItems(items)
        finally:
            dlg.Destroy()
        event.Skip()

    def OnFrame1Activate(self, event):
        self.rbTotGen.Value=True
        event.Skip()

    def OnRbTotGenRadiobutton(self, event):
        self.lcResultado.Show()
        self.lcResultFecha.Hide()
        event.Skip()

    def OnRbTotFechaRadiobutton(self, event):
        self.lcResultado.Hide()
        self.lcResultFecha.Show()
        event.Skip()

    
