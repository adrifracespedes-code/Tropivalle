/**
 * TropiValle — Asistente de voz (Web Speech API)
 * Voz femenina en español + comandos para abrir secciones y la app.
 */
(function () {
  'use strict';

  var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
  var synth = window.speechSynthesis;
  var femaleVoice = null;
  var isListening = false;
  var panelOpen = false;

  function loadVoices() {
    if (!synth) return;
    var voices = synth.getVoices() || [];
    // Preferir voz femenina en español
    var preferred = [
      /female/i,
      /femenina/i,
      /sabina/i,
      /lucia/i,
      /lucía/i,
      /paulina/i,
      /helena/i,
      /monica/i,
      /mónica/i,
      /francisca/i,
      /dalia/i,
      /google español/i,
      /microsoft sabina/i,
      /microsoft helena/i
    ];

    var esVoices = voices.filter(function (v) {
      return v.lang && v.lang.toLowerCase().indexOf('es') === 0;
    });

    femaleVoice = null;
    for (var i = 0; i < preferred.length; i++) {
      var re = preferred[i];
      for (var j = 0; j < esVoices.length; j++) {
        if (re.test(esVoices[j].name)) {
          femaleVoice = esVoices[j];
          break;
        }
      }
      if (femaleVoice) break;
    }
    if (!femaleVoice && esVoices.length) {
      // Heurística: muchas voces "es-ES" / "es-MX" sin "Male" en el nombre
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

  function speak(text, onEnd) {
    if (!synth || !text) {
      if (onEnd) onEnd();
      return;
    }
    try {
      synth.cancel();
      var u = new SpeechSynthesisUtterance(text);
      u.lang = (femaleVoice && femaleVoice.lang) || 'es-ES';
      u.rate = 0.95;
      u.pitch = 1.15; // un poco más agudo → más “femenino” si no hay voz dedicada
      u.volume = 1;
      if (femaleVoice) u.voice = femaleVoice;
      u.onend = function () {
        if (onEnd) onEnd();
      };
      setStatus('Asistente: ' + text);
      synth.speak(u);
    } catch (e) {
      console.warn(e);
      if (onEnd) onEnd();
    }
  }

  function normalize(s) {
    return (s || '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[¿?¡!.,]/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  function go(url, message) {
    speak(message || 'Abriendo…', function () {
      window.location.href = url;
    });
  }

  function openExternal(url, message) {
    speak(message || 'Abriendo…', function () {
      window.open(url, '_blank', 'noopener');
    });
  }

  function scrollToId(id, message) {
    var el = document.getElementById(id);
    if (!el) {
      // intentar en home
      if (location.pathname !== '/' && location.pathname !== '/Home' && location.pathname.indexOf('/Home/') !== 0) {
        go('/#' + id, message || ('Voy a ' + id));
        return;
      }
      speak('No encontré esa sección en esta página.');
      return;
    }
    speak(message || 'Mostrando la sección');
    var top = el.getBoundingClientRect().top + window.pageYOffset - 80;
    window.scrollTo({ top: top, behavior: 'smooth' });
  }

  function handleCommand(raw) {
    var t = normalize(raw);
    setStatus('Tú: « ' + raw + ' »');

    if (!t) {
      speak('No te escuché. Prueba de nuevo.');
      return;
    }

    // Saludos
    if (/^(hola|buenos dias|buenas tardes|buenas noches|hey|ola)\b/.test(t) || t === 'hola tropivalle') {
      speak(
        'Hola, soy tu asistente de TropiValle. Puedo abrir la tienda, el inicio, contacto, o secciones de la página. Di ayuda para escuchar los comandos.'
      );
      return;
    }

    // Ayuda
    if (/\b(ayuda|comandos|que puedes hacer|menu de voz)\b/.test(t)) {
      speak(
        'Puedes decir: abrir inicio, abrir tienda, abrir catálogo, néctar, agua, contacto, WhatsApp, ubicación, o instalar la aplicación. También: leer esta página.'
      );
      return;
    }

    // Abrir / ir a — Inicio
    if (
      /\b(abrir|abre|ir a|ve a|vamos a|mostrar|muestra)?\s*(el )?inicio\b/.test(t) ||
      /\b(pagina principal|home)\b/.test(t)
    ) {
      go('/', 'Abriendo el inicio de TropiValle.');
      return;
    }

    // Tienda / catálogo / productos
    if (
      /\b(abrir|abre|ir a|ve a|vamos a|mostrar)?\s*(la )?(tienda|catalogo|productos|comprar)\b/.test(t) ||
      /\b(quiero comprar|ver productos)\b/.test(t)
    ) {
      go('/Products', 'Abriendo la tienda y el catálogo de productos.');
      return;
    }

    // Néctar
    if (/\b(nectar|jugos?|tropivalle nectar)\b/.test(t)) {
      go(
        '/Products?category=' + encodeURIComponent('Nectar TropiValle'),
        'Abriendo los néctares TropiValle.'
      );
      return;
    }

    // Agua
    if (/\b(agua|agua de mesa)\b/.test(t)) {
      go(
        '/Products?category=' + encodeURIComponent('Agua de Mesa'),
        'Abriendo el agua de mesa.'
      );
      return;
    }

    // Contacto / WhatsApp / teléfono
    if (/\b(whatsapp|wasap|watsap)\b/.test(t)) {
      openExternal(
        'https://wa.me/59175950776?text=' + encodeURIComponent('Hola TropiValle, quiero información'),
        'Abriendo WhatsApp de TropiValle.'
      );
      return;
    }
    if (/\b(llamar|telefono|contacto)\b/.test(t)) {
      speak('Abriendo el teléfono de contacto.', function () {
        window.location.href = 'tel:+59175950776';
      });
      return;
    }

    // Ubicación / mapa
    if (/\b(ubicacion|mapa|direccion|donde estan|como llegar)\b/.test(t)) {
      openExternal(
        'https://maps.app.goo.gl/TuPeeuGRz8Dcdhto9',
        'Abriendo la ubicación en el mapa.'
      );
      return;
    }

    // Secciones del home (anclas)
    if (/\b(nosotros|quienes somos|sobre nosotros)\b/.test(t)) {
      scrollToId('nosotros', 'Mostrando la sección Nosotros.');
      return;
    }
    if (/\b(ingredientes)\b/.test(t)) {
      scrollToId('ingredientes', 'Mostrando ingredientes.');
      return;
    }
    if (/\b(sostenibilidad|sustentabilidad)\b/.test(t)) {
      scrollToId('sostenibilidad', 'Mostrando sostenibilidad.');
      return;
    }

    // Ventas (admin)
    if (/\b(ventas|reporte|grafica)\b/.test(t)) {
      if (document.body.getAttribute('data-is-admin') === 'true') {
        go('/Sales', 'Abriendo el panel de ventas.');
      } else {
        speak('El panel de ventas es solo para administradores. Puedo abrirte la tienda si quieres.');
      }
      return;
    }

    // Instalar PWA / app
    if (/\b(instalar|instala|agregar a la pantalla|instalar (la )?app|instalar (la )?aplicacion)\b/.test(t)) {
      var installBtn = document.getElementById('pwaInstallBtn');
      if (installBtn && installBtn.style.display !== 'none') {
        speak('Abriendo la instalación de la aplicación TropiValle.');
        installBtn.click();
      } else {
        speak(
          'Para instalar la app, en Chrome usa el menú y elige Instalar TropiValle, o Añadir a la pantalla de inicio en el móvil. La app funciona mejor con conexión segura.'
        );
      }
      return;
    }

    // Leer página
    if (/\b(leer|lee|escuchar|que hay aqui)\b/.test(t)) {
      var h1 = document.querySelector('h1, .hero-title, .brand-text');
      var title = (h1 && h1.innerText) || document.title || 'TropiValle';
      speak(
        'Estás en ' +
          title.trim() +
          '. TropiValle, néctares y agua de mesa de Cochabamba. Di abrir tienda para ver productos, o ayuda para más comandos.'
      );
      return;
    }

    // Detener
    if (/\b(callate|silencio|stop|parar|basta)\b/.test(t)) {
      if (synth) synth.cancel();
      setStatus('');
      return;
    }

    speak(
      'No entendí del todo. Prueba: abrir tienda, abrir inicio, néctar, agua, contacto, o instalar la aplicación. Di ayuda para la lista completa.'
    );
  }

  function setStatus(msg) {
    var el = document.getElementById('tvAsistenteStatus');
    if (el) el.textContent = msg || '';
  }

  function setListeningUi(on) {
    isListening = on;
    var btn = document.getElementById('tvAsistenteMic');
    var panel = document.getElementById('tvAsistente');
    if (btn) btn.classList.toggle('listening', on);
    if (panel) panel.classList.toggle('listening', on);
  }

  function startListening() {
    if (!SpeechRecognition) {
      speak('Tu navegador no soporta reconocimiento de voz. Usa Chrome o Edge.');
      setStatus('Reconocimiento no disponible en este navegador.');
      return;
    }
    if (isListening) return;

    var recognition = new SpeechRecognition();
    recognition.lang = 'es-BO';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;
    recognition.continuous = false;

    recognition.onstart = function () {
      setListeningUi(true);
      setStatus('Escuchando… habla ahora');
    };
    recognition.onend = function () {
      setListeningUi(false);
    };
    recognition.onerror = function (ev) {
      setListeningUi(false);
      if (ev.error === 'not-allowed') {
        speak('Necesito permiso del micrófono para escucharte.');
        setStatus('Permiso de micrófono denegado.');
      } else if (ev.error !== 'aborted') {
        setStatus('Error: ' + ev.error);
      }
    };
    recognition.onresult = function (ev) {
      var text = ev.results[0][0].transcript;
      handleCommand(text);
    };

    try {
      recognition.start();
    } catch (e) {
      setListeningUi(false);
      setStatus('No se pudo iniciar el micrófono.');
    }
  }

  function togglePanel() {
    var panel = document.getElementById('tvAsistente');
    if (!panel) return;
    panelOpen = !panelOpen;
    panel.classList.toggle('open', panelOpen);
    if (panelOpen) {
      speak('Asistente de TropiValle listo. ¿Qué quieres abrir?');
    }
  }

  function injectUi() {
    if (document.getElementById('tvAsistente')) return;

    var wrap = document.createElement('div');
    wrap.id = 'tvAsistente';
    wrap.className = 'tv-asistente no-print';
    wrap.innerHTML =
      '<div class="tv-asistente-panel" id="tvAsistentePanel">' +
      '  <div class="tv-asistente-header">' +
      '    <span class="tv-asistente-avatar" aria-hidden="true">🎙️</span>' +
      '    <div><strong>Asistente TropiValle</strong><small>Voz · abre tienda, inicio y más</small></div>' +
      '    <button type="button" class="tv-asistente-close" id="tvAsistenteClose" aria-label="Cerrar">×</button>' +
      '  </div>' +
      '  <p class="tv-asistente-hint">Prueba: <em>abrir tienda</em>, <em>néctar</em>, <em>contacto</em>, <em>instalar app</em></p>' +
      '  <div class="tv-asistente-status" id="tvAsistenteStatus" aria-live="polite"></div>' +
      '  <div class="tv-asistente-actions">' +
      '    <button type="button" class="tv-asistente-mic" id="tvAsistenteMic" title="Hablar">' +
      '      <i class="bi bi-mic-fill"></i> Hablar' +
      '    </button>' +
      '    <button type="button" class="tv-asistente-help" id="tvAsistenteHelp">Ayuda</button>' +
      '  </div>' +
      '</div>' +
      '<button type="button" class="tv-asistente-fab" id="tvAsistenteFab" title="Asistente de voz" aria-label="Asistente de voz">' +
      '  <i class="bi bi-soundwave"></i>' +
      '</button>';

    document.body.appendChild(wrap);

    document.getElementById('tvAsistenteFab').addEventListener('click', togglePanel);
    document.getElementById('tvAsistenteClose').addEventListener('click', function () {
      panelOpen = false;
      document.getElementById('tvAsistente').classList.remove('open');
      if (synth) synth.cancel();
    });
    document.getElementById('tvAsistenteMic').addEventListener('click', startListening);
    document.getElementById('tvAsistenteHelp').addEventListener('click', function () {
      handleCommand('ayuda');
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    injectUi();
    window.TropivalleAsistente = {
      speak: speak,
      listen: startListening,
      open: function () {
        panelOpen = true;
        var p = document.getElementById('tvAsistente');
        if (p) p.classList.add('open');
      }
    };
  });
})();
