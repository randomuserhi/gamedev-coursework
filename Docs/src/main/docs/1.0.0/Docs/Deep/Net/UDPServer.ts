RHU.require(new Error(), { 
    docs: "docs", rhuDocuscript: "docuscript",
}, function({
    docs, rhuDocuscript,
}) {
    docs.jit = (version, path) => docuscript<RHUDocuscript.Language, RHUDocuscript.FuncMap>(({
        p, h, frag, br, icode, code, link, pl, ot
    }) => {
        h(1, "Definition");
        p("Namespace: ", icode([], "Deep"));
        br();
        p("Creates a UDP server.");
        code(["csharp"], "public class UDPServer");

        h(1, "Constructors");
        ot({ widths: ["33%"] }, 
            ["signature", "summary"],
            {
                signature: pl([`${path}/Constructors`], "UDPServer(ArraySegment<byte>)"),
                summary: "Initialises a UDPServer with a given memory buffer.",
            },
        );

        h(1, "Methods");
        ot({ widths: ["33%"] }, 
            ["signature", "summary"],
            {
                signature: pl([`${path}/Bind`], "Bind(EndPoint)"),
                summary: "Binds a socket to a remote end point.",
            },
            {
                signature: pl([`${path}/Disconnect`], "Disconnect()"),
                summary: "Disconnects and disposes of the server and its internal socket.",
            },
            {
                signature: pl([`${path}/Disconnect`], "Dispose()"),
                summary: "Disconnects and disposes of the server and its internal socket.",
            },
        );
    }, rhuDocuscript);
});