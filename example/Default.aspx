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
    <% if (lookup != null) { %>
        <h2>Lookup response</h2>
        <%= lookup.ToJson() %>
    <% } %>

    <h2>Where you are</h2>
    <%= location.ToJson() %>

    <h2>Update</h2>
    
    <p>Enter a location below and click "Move!" to update.</p>

    <form method="POST">
        <p><label for="free-text-entry">Free-text entry:</label> <input type="text" name="q" id="free-text-entry" size="40"></p>
        <p><label for="place-id">Place ID:</label> <input type="text" name="place_id" id="place-id" size="40"></p>
        <p><label for="woeid">WOEID:</label> <input type="text" name="woeid" id="woeid" size="10"></p>
        <p><label for="lat">Lat:</label> <input type="text" name="lat" id="lat" size="10"> <label for="lon">Lon:</label> <input type="text" name="lon" size="10"></p>
        <input type="submit" name="submit" value="Move!">
        or just check your query: <input type="submit" name="submit" value="Lookup">
    </form>
<% } %>
</body>
</html>
