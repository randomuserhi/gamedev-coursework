declare namespace RHU {
    interface Modules {
        "docs/indices": {
            DOCUSCRIPT_ROOT: string;
        };
    }
}

(() => {
    let DOCUSCRIPT_ROOT = "";
    if (document.currentScript) {
        let s = document.currentScript as HTMLScriptElement;
        let r = s.src.match(/(.*)[/\\]/);
        if (r)
            DOCUSCRIPT_ROOT = r[1] || "";
    } else {
        throw new Error("Failed to get document root.");
    }

    RHU.module(new Error(), "docs/indices", { 
        docs: "docs",
    }, function({
        docs,
    }) {
        
        ((docs: Docs) => {
            const stack: string[] = [];
            const dir = (dir: string, func: (docs: (path: string, page?: string, index?: number) => void) => void) => {
                stack.push(dir);
                const current = [...stack];
                const d = (path: string, page?: string, index?: number) => {
                    docs.set(`${[...current, ...path.split("/")].join("/")}`, page, index);
                };
                func(d);
                stack.pop();
            };

            docs.set("About", "About.js", 0);
            docs.set("About/cringe", undefined, 0);
            docs.set("About/cringe/bruh", undefined, 0);
            docs.set("About/bruh", undefined, 0);
            docs.set("About/bruh/cringe", undefined, 0);
            docs.set("Docs/cringe", undefined, 0);
            docs.set("Docs/cringe/bruh", undefined, 0);
            docs.set("Docs/bruh", undefined, 0);
            docs.set("Docs/bruh/cringe", undefined, 0);
            docs.set(`Docs`, undefined, 1);
            dir("Docs", (set) => {
                set(`Deep`, `Docs/Deep.js`, 0);
            });
        })(docs.create("1.0.0", "About"));

        return {
            DOCUSCRIPT_ROOT
        };
    });
})();