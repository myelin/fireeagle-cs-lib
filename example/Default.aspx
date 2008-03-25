<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Fire Eagle C# library demo</title>
</head>
<body>
<% if (!authorized) { %>
    <p><a href="?f=start">Click here to authenticate with FireEagle!</a></p>
<% } else { %>
    <p>you're authenticated!</p>
    <%= location.ToJson() %>
<% } %>
</body>
</html>
