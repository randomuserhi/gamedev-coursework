RHU.require(new Error(), { 
    docs: "docs", rhuDocuscript: "docuscript",
}, function({
    docs, rhuDocuscript,
}) {
    docs.jit = (version, path) => docuscript<RHUDocuscript.Language, RHUDocuscript.FuncMap>(({
        p, frag, br, link, t
    }) => {
        frag(
            p(
                "test",
            )
        );

        t(["33%"], 
            ["item", "item"],
            ["item", "item"]
        );
    }, rhuDocuscript);
});