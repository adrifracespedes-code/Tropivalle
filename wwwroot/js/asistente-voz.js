/**
 * TropiValle — Asistente de voz
 * Sinónimos universales + palabras de la página + tolerancia a errores de reconocimiento.
 */
(function () {
    "use strict";

    var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    var synth = window.speechSynthesis;
    var femaleVoice = null;
    var isListening = false;
    var panelOpen = false;

    /* ---------- Voz femenina ---------- */
    function loadVoices() {
        if (!synth) return;
        var voices = synth.getVoices() || [];
        var preferred = [
            /female/i, /femenina/i, /sabina/i, /lucia/i, /lucía/i,
            /paulina/i, /helena/i, /monica/i, /mónica/i, /francisca/i,
            /dalia/i, /google español/i, /microsoft sabina/i, /microsoft helena/i
        ];
        var esVoices = voices.filter(function (v) {
            return v.lang && v.lang.toLowerCase().indexOf("es") === 0;
        });
        femaleVoice = null;
        for (var i = 0; i < preferred.length; i++) {
            for (var j = 0; j < esVoices.length; j++) {
                if (preferred[i].test(esVoices[j].name)) {
                    femaleVoice = esVoices[j];
                    break;
                }
            }
            if (femaleVoice) break;
        }
        if (!femaleVoice && esVoices.length) {
            femaleVoice =
                esVoices.find(function (v) {
                    return !/male|hombre|jorge|pablo|carlos/i.test(v.name);
                }) || esVoices[0];
        }
    }
    if (synth) {
        loadVoices();
        synth.onvoiceschanged = loadVoices;
    }

    function setStatus(msg) {
        var el = document.getElementById("tvAsistenteStatus");
        if (el) el.textContent = msg || "";
    }

    function setListeningUi(on) {
        isListening = on;
        var btn = document.getElementById("tvAsistenteMic");
        var panel = document.getElementById("tvAsistente");
        if (btn) btn.classList.toggle("listening", on);
        if (panel) panel.classList.toggle("listening", on);
    }

    function speak(text, onEnd) {
        if (!synth || !text) {
            if (onEnd) onEnd();
            return;
        }
        try {
            synth.cancel();
            var u = new SpeechSynthesisUtterance(text);
            u.lang = (femaleVoice && femaleVoice.lang) || "es-ES";
            u.rate = 0.95;
            u.pitch = 1.15;
            u.volume = 1;
            if (femaleVoice) u.voice = femaleVoice;
            u.onend = function () {
                if (onEnd) onEnd();
            };
            setStatus("Asistente: " + text);
            synth.speak(u);
        } catch (e) {
            if (onEnd) onEnd();
        }
    }

    /* ---------- Normalización + tolerancia a errores ---------- */
    function normalize(s) {
        return (s || "")
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/[¿?¡!.,;:()"']/g, " ")
            .replace(/\s+/g, " ")
            .trim();
    }

    var STT_FIXES = [
        [/necta\s*r?/g, "nectar"],
        [/nektar|nectar|nectares|nectars/g, "nectar"],
        [/jugo?s?/g, "jugo"],
        [/tropi\s*valle|tropival|tropi valle|tro pi valle|tropi val/g, "tropivalle"],
        [/wasap|watsap|guasap|whats app|whatssap|guasap/g, "whatsapp"],
        [/catalago|catalogo|cata logo/g, "catalogo"],
        [/produtos|produktos|productos/g, "productos"],
        [/tiend[ae]|tien da/g, "tienda"],
        [/insiio|insio|inisio|prinsipio|prinsipal/g, "inicio"],
        [/ubicasion|ubicacion|direccion|direksion/g, "ubicacion"],
        [/kontakto|kontacto|contacto/g, "contacto"],
        [/telefone|telefono|llamada/g, "telefono"],
        [/agua de mesa|aguademesa|agua mesa/g, "agua"],
        [/instalar|instala|instala r|instala la app|instalar app/g, "instalar"],
        [/komprar|conprar|comprar/g, "comprar"],
        [/ventas|benta|bentas/g, "ventas"],
        [/ayuda|aiuda|ayda/g, "ayuda"],
        [/hola|ola|alo/g, "hola"]
    ];

    function fixSpeechErrors(t) {
        var s = normalize(t);
        for (var i = 0; i < STT_FIXES.length; i++) {
            s = s.replace(STT_FIXES[i][0], STT_FIXES[i][1]);
        }
        return s.replace(/\s+/g, " ").trim();
    }

    function lev(a, b) {
        a = normalize(a);
        b = normalize(b);
        if (a === b) return 0;
        if (!a.length) return b.length;
        if (!b.length) return a.length;
        var m = [];
        for (var i = 0; i <= b.length; i++) m[i] = [i];
        for (var j = 0; j <= a.length; j++) m[0][j] = j;
        for (i = 1; i <= b.length; i++) {
            for (j = 1; j <= a.length; j++) {
                m[i][j] =
                    b.charAt(i - 1) === a.charAt(j - 1)
                        ? m[i - 1][j - 1]
                        : Math.min(m[i - 1][j - 1] + 1, m[i][j - 1] + 1, m[i - 1][j] + 1);
            }
        }
        return m[b.length][a.length];
    }

    function fuzzyIncludes(text, word, maxDist) {
        maxDist = maxDist == null ? 2 : maxDist;
        if (!word) return false;
        if (text.indexOf(word) !== -1) return true;
        var parts = text.split(" ");
        for (var i = 0; i < parts.length; i++) {
            var w = parts[i];
            if (w.length < 3 && word.length >= 4) continue;
            var allow = maxDist;
            if (word.length <= 4) allow = 1;
            if (lev(w, word) <= allow) return true;
        }
        return false;
    }

    function anyMatch(text, words, maxDist) {
        for (var i = 0; i < words.length; i++) {
            if (fuzzyIncludes(text, normalize(words[i]), maxDist)) return true;
        }
        return false;
    }

    var WORDS = {
        inicio: [
            "inicio", "home", "principal", "portada", "empezar", "comienzo",
            "pagina principal", "volver al inicio", "menu principal"
        ],
        tienda: [
            "tienda", "catalogo", "productos", "comprar", "compra", "shop",
            "ver productos", "quiero comprar", "ofertas", "stock", "listado",
            "nectar tropivalle", "comprar ahora"
        ],
        nectar: [
            "nectar", "nectares", "jugo", "jugos", "refresco", "refrescos",
            "tropivalle", "durazno", "manzana", "tumbo", "litro", "retornable",
            "descartable", "caja", "paquete"
        ],
        agua: [
            "agua", "agua de mesa", "botellon", "botellón", "mesa", "purificada"
        ],
        contacto: [
            "contacto", "contactar", "telefono", "llamar", "llame", "numero",
            "celular", "comunicar", "hablar con"
        ],
        whatsapp: [
            "whatsapp", "wasap", "mensaje", "escribir", "chat"
        ],
        ubicacion: [
            "ubicacion", "mapa", "direccion", "donde estan", "como llegar",
            "cochabamba", "chimba", "salazar", "google maps", "maps"
        ],
        instalar: [
            "instalar", "instala", "app", "aplicacion", "pantalla de inicio",
            "agregar", "descargar app", "instalar aplicacion"
        ],
        ventas: ["ventas", "reporte", "grafica", "graficas", "estadisticas", "admin"],
        ayuda: [
            "ayuda", "comandos", "que puedes hacer", "opciones", "menu de voz",
            "instrucciones", "como funciona"
        ],
        leer: ["leer", "lee", "escuchar", "que hay aqui", "explica", "describe"],
        silencio: ["callate", "silencio", "stop", "parar", "basta", "calla", "quieto"],
        hola: ["hola", "buenos dias", "buenas tardes", "buenas noches", "hey", "buenas"]
    };

    function matchIntent(t) {
        if (anyMatch(t, WORDS.silencio, 1)) return "silencio";
        if (anyMatch(t, WORDS.hola, 1) && t.split(" ").length <= 4) return "hola";
        if (anyMatch(t, WORDS.ayuda, 1)) return "ayuda";
        if (anyMatch(t, WORDS.whatsapp, 1)) return "whatsapp";
        if (anyMatch(t, WORDS.ubicacion, 1)) return "ubicacion";
        if (anyMatch(t, WORDS.contacto, 1) || anyMatch(t, ["telefono", "llamar"], 1))
            return "contacto";
        if (anyMatch(t, WORDS.instalar, 1)) return "instalar";
        if (anyMatch(t, WORDS.ventas, 1)) return "ventas";
        if (anyMatch(t, WORDS.agua, 1)) return "agua";
        if (anyMatch(t, WORDS.nectar, 2)) return "nectar";
        if (anyMatch(t, WORDS.tienda, 1)) return "tienda";
        if (anyMatch(t, WORDS.inicio, 1)) return "inicio";
        if (anyMatch(t, WORDS.leer, 1)) return "leer";

        if (/\b(abrir|abre|ir a|ve a|vamos|mostrar|muestra|quiero|ver)\b/.test(t)) {
            if (anyMatch(t, WORDS.tienda.concat(["tienda", "catalogo"]), 2)) return "tienda";
            if (anyMatch(t, WORDS.inicio, 2)) return "inicio";
            if (anyMatch(t, WORDS.nectar, 2)) return "nectar";
            if (anyMatch(t, WORDS.agua, 1)) return "agua";
        }
        return null;
    }

    function go(url, message) {
        speak(message || "Abriendo…", function () {
            window.location.href = url;
        });
    }

    function openExternal(url, message) {
        speak(message || "Abriendo…", function () {
            window.open(url, "_blank", "noopener");
        });
    }

    function pageTextHints() {
        var nodes = document.querySelectorAll(
            "h1, h2, h3, .nav-item-link, .btn-buy, .brand-text, .product-title, .card-title, a.nav-link"
        );
        var bag = [];
        for (var i = 0; i < nodes.length && bag.length < 40; i++) {
            var tx = normalize(nodes[i].innerText || "");
            if (tx && tx.length > 2 && tx.length < 40) bag.push(tx);
        }
        return bag;
    }

    function tryPageWords(t) {
        var hints = pageTextHints();
        for (var i = 0; i < hints.length; i++) {
            var h = hints[i];
            if (t.indexOf(h) !== -1 || fuzzyIncludes(t, h.split(" ")[0], 1)) {
                if (/tienda|producto|catalogo|comprar/.test(h)) return "tienda";
                if (/inicio|home/.test(h)) return "inicio";
                if (/contacto|whatsapp|llamar/.test(h)) return "contacto";
                if (/nectar|jugo|tropivalle/.test(h)) return "nectar";
                if (/agua/.test(h)) return "agua";
                if (/venta/.test(h)) return "ventas";
            }
        }
        return null;
    }

    function handleCommand(raw) {
        var t = fixSpeechErrors(raw);
        setStatus("Tú: « " + raw + " »");

        if (!t) {
            speak("No te escuché bien. Prueba otra vez, un poco más cerca del micrófono.");
            return;
        }

        var intent = matchIntent(t) || tryPageWords(t);

        switch (intent) {
            case "hola":
                speak(
                    "Hola, soy la asistente de TropiValle. Puedes decir: tienda, néctar, agua, contacto, inicio o instalar app."
                );
                return;
            case "ayuda":
                speak(
                    "Comandos: inicio, tienda, productos, néctar, jugo, agua, contacto, WhatsApp, ubicación, instalar aplicación, leer. También puedes decir comprar ahora."
                );
                return;
            case "inicio":
                go("/", "Abriendo el inicio.");
                return;
            case "tienda":
                go("/Products", "Abriendo la tienda.");
                return;
            case "nectar":
                go(
                    "/Products?category=" + encodeURIComponent("Nectar TropiValle"),
                    "Abriendo néctares TropiValle."
                );
                return;
            case "agua":
                go(
                    "/Products?category=" + encodeURIComponent("Agua de Mesa"),
                    "Abriendo agua de mesa."
                );
                return;
            case "whatsapp":
                openExternal(
                    "https://wa.me/59175950776?text=" +
                    encodeURIComponent("Hola TropiValle, quiero información"),
                    "Abriendo WhatsApp."
                );
                return;
            case "contacto":
                speak("Abriendo el teléfono de contacto.", function () {
                    window.location.href = "tel:+59175950776";
                });
                return;
            case "ubicacion":
                openExternal(
                    "https://maps.app.goo.gl/TuPeeuGRz8Dcdhto9",
                    "Abriendo la ubicación en el mapa."
                );
                return;
            case "instalar":
                var installBtn = document.getElementById("pwaInstallBtn");
                if (installBtn && installBtn.style.display !== "none") {
                    speak("Abriendo la instalación de la aplicación.");
                    installBtn.click();
                } else {
                    speak(
                        "Para instalar, en Chrome elige Instalar TropiValle, o en el móvil Añadir a la pantalla de inicio."
                    );
                }
                return;
            case "ventas":
                if (document.body.getAttribute("data-is-admin") === "true") {
                    go("/Sales", "Abriendo ventas.");
                } else {
                    speak("Ventas es solo para administradores. ¿Quieres que abra la tienda?");
                }
                return;
            case "leer":
                var h1 = document.querySelector("h1, .hero-title, .brand-text");
                var title = (h1 && h1.innerText) || document.title || "TropiValle";
                speak(
                    "Estás en " +
                    title.trim() +
                    ". TropiValle, néctares y agua de mesa. Di tienda, néctar o agua para navegar."
                );
                return;
            case "silencio":
                if (synth) synth.cancel();
                setStatus("");
                return;
            default:
                speak(
                    "No entendí. Di por ejemplo: tienda, néctar, agua, contacto o ayuda. Habla claro y cerca del micrófono."
                );
        }
    }

    function startListening() {
        if (!SpeechRecognition) {
            speak("Tu navegador no soporta reconocimiento de voz. Usa Chrome o Edge.");
            setStatus("No disponible en este navegador.");
            return;
        }
        if (isListening) return;

        var recognition = new SpeechRecognition();
        recognition.lang = "es-BO";
        recognition.interimResults = false;
        recognition.maxAlternatives = 3;
        recognition.continuous = false;

        recognition.onstart = function () {
            setListeningUi(true);
            setStatus("Escuchando…");
        };
        recognition.onend = function () {
            setListeningUi(false);
        };
        recognition.onerror = function (ev) {
            setListeningUi(false);
            if (ev.error === "not-allowed") {
                speak("Permite el micrófono para usar la voz.");
                setStatus("Micrófono bloqueado.");
            } else if (ev.error === "no-speech") {
                setStatus("No se escuchó nada. Intenta de nuevo.");
                speak("No escuché nada. Prueba otra vez.");
            } else if (ev.error !== "aborted") {
                setStatus("Error: " + ev.error);
            }
        };
        recognition.onresult = function (ev) {
            var best = "";
            var alts = [];
            try {
                var res = ev.results[0];
                for (var i = 0; i < res.length; i++) {
                    alts.push(res[i].transcript);
                }
                best = alts[0] || "";
            } catch (e) {
                best = ev.results[0][0].transcript;
            }
            var fixed = fixSpeechErrors(best);
            var intent = matchIntent(fixed) || tryPageWords(fixed);
            if (!intent && alts.length > 1) {
                for (var k = 1; k < alts.length; k++) {
                    var f2 = fixSpeechErrors(alts[k]);
                    intent = matchIntent(f2) || tryPageWords(f2);
                    if (intent) {
                        best = alts[k];
                        break;
                    }
                }
            }
            handleCommand(best);
        };

        try {
            recognition.start();
        } catch (e) {
            setListeningUi(false);
            setStatus("No se pudo iniciar el micrófono.");
        }
    }

    function togglePanel() {
        var panel = document.getElementById("tvAsistente");
        if (!panel) return;
        panelOpen = !panelOpen;
        panel.classList.toggle("open", panelOpen);
        if (panelOpen) {
            speak("Asistente lista. ¿Qué quieres abrir?");
        }
    }

    function injectUi() {
        if (document.getElementById("tvAsistente")) return;
        var wrap = document.createElement("div");
        wrap.id = "tvAsistente";
        wrap.className = "tv-asistente no-print";
        wrap.innerHTML =
            '<div class="tv-asistente-panel">' +
            '  <div class="tv-asistente-header">' +
            '    <span class="tv-asistente-avatar" aria-hidden="true">🎙️</span>' +
            "    <div><strong>Asistente TropiValle</strong><small>Di tienda, néctar, agua, contacto…</small></div>" +
            '    <button type="button" class="tv-asistente-close" id="tvAsistenteClose" aria-label="Cerrar">×</button>' +
            "  </div>" +
            '  <p class="tv-asistente-hint">Ejemplos: <em>tienda</em>, <em>néctar</em>, <em>agua</em>, <em>WhatsApp</em>, <em>ayuda</em></p>' +
            '  <div class="tv-asistente-status" id="tvAsistenteStatus" aria-live="polite"></div>' +
            '  <div class="tv-asistente-actions">' +
            '    <button type="button" class="tv-asistente-mic" id="tvAsistenteMic"><i class="bi bi-mic-fill"></i> Hablar</button>' +
            '    <button type="button" class="tv-asistente-help" id="tvAsistenteHelp">Ayuda</button>' +
            "  </div>" +
            "</div>" +
            '<button type="button" class="tv-asistente-fab" id="tvAsistenteFab" title="Asistente de voz" aria-label="Asistente">' +
            '  <i class="bi bi-soundwave"></i>' +
            "</button>";
        document.body.appendChild(wrap);
        document.getElementById("tvAsistenteFab").addEventListener("click", togglePanel);
        document.getElementById("tvAsistenteClose").addEventListener("click", function () {
            panelOpen = false;
            document.getElementById("tvAsistente").classList.remove("open");
            if (synth) synth.cancel();
        });
        document.getElementById("tvAsistenteMic").addEventListener("click", startListening);
        document.getElementById("tvAsistenteHelp").addEventListener("click", function () {
            handleCommand("ayuda");
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        injectUi();
        window.TropivalleAsistente = { speak: speak, listen: startListening };
    });
})();