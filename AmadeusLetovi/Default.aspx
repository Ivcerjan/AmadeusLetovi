<%@ Page Title="Pretraga letova" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AmadeusLetovi._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Pretraga letova</h2>
    <div class="jumbotron">
        <h4>Mjesto polaska</h4>
        <asp:DropDownList runat="server" CssClass="form-control" ID="ddlPolazak" />
        <h4>Mjesto dolaska</h4>
        <asp:DropDownList runat="server" CssClass="form-control" ID="ddlDolazak">
        </asp:DropDownList>
        <h4>Datum polaska</h4>
        <input runat="server" ClientIDMode="Static" type="text" class="form-control" placeholder="Odaberite datum" name="dPickBootstrap" id="datePickerBoot" >
        <h4>Datum povratka</h4>
        <input runat="server" ClientIDMode="Static" type="text" class="form-control" placeholder="Odaberite datum" name="dPickBootstrapPovratak" id="datePickerBootPovratak" />
        <h4>Broj putnika</h4>
        <asp:TextBox ID="txtBrojPutnika" TextMode="Number" CssClass="form-control" runat="server" min="0" max="6" step="1" />
        <h4>Valuta</h4>
        <asp:DropDownList runat="server" CssClass="form-control" ID="ddlValuta">
            <asp:ListItem Text="EUR" Value="EUR" />
            <asp:ListItem Text="USD" Value="USD" />
            <asp:ListItem Text="HRK" Value="HRK" />
        </asp:DropDownList>
        <br />
        <asp:Button Text="Pretraži" CssClass="btn btn-primary" ID="btnPretragaLetova" runat="server" OnClick="btnPretragaLetova_Click" />
        <br />
        <asp:Label ID="lblGreska" runat="server" ForeColor="#ff3300" Font-Size="Larger"></asp:Label>
        <br />
    </div>
    <div class="container-md">
        <asp:GridView runat="server" AutoGenerateColumns="true" ID="gvJsonPodaciZaPrikaz" CssClass="table table-hover table-striped">
        </asp:GridView>
    </div>

    <script src="Scripts/jquery-3.4.1.min.js"></script>
    <script src="Scripts/bootstrap.js"></script>
    <script src="Scripts/bootstrap-datepicker.min.js"></script>
    <script src="Scripts/moment.min.js"></script>
    <link href="Content/bootstrap.css" rel="stylesheet" />
    <link href="Content/bootstrap-datepicker.css" rel="stylesheet" />

    <script type="text/javascript">
        $(document).ready(function () {
            $('#datePickerBoot').datepicker({
                format: "yyyy-mm-dd",
                language: "hr"
            });
            $('#datePickerBootPovratak').datepicker({
                format: "yyyy-mm-dd",
                language: "hr"
            });
        });
    </script>
</asp:Content>

