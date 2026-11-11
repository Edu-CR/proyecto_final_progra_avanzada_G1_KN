(function () {
    'use strict';

    // Mismas reglas que el controlador (si cambia una, cambiarla en los dos lados)
    var REGEX_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    var REGEX_ID = /^[0-9]{8,20}$/;
    var REGEX_CONTRASENNA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{8,15}$/;

    var campos = {
        identificacion: document.getElementById('identificacion'),
        nombreCompleto: document.getElementById('nombreCompleto'),
        correoElectronico: document.getElementById('correoElectronico'),
        idRol: document.getElementById('idRol'),
        contrasenna: document.getElementById('contrasenna'),
        confirmarContrasenna: document.getElementById('confirmarContrasenna')
    };

    var form = campos.identificacion.closest('form');
    var boton = form.querySelector('button[type="submit"]');

    // Cada regla devuelve un mensaje de error, o "" si el campo está bien
    var reglas = {
        nombreCompleto: function (v) {
            return v.trim() === '' ? 'El nombre completo es obligatorio.' : '';
        },
        correoElectronico: function (v) {
            v = v.trim();
            if (v === '') return 'El correo es obligatorio.';
            if (!REGEX_CORREO.test(v)) return 'El correo no tiene un formato válido.';
            return '';
        },
        identificacion: function (v) {
            v = v.trim();
            if (v === '') return 'El código es obligatorio.';
            if (!REGEX_ID.test(v)) return 'La identificacion es de maximo 20 dígitos.';
            return '';
        },
        idRol: function (v) {
            return v === '' ? 'Seleccione un rol.' : '';
        },
        contrasenna: function (v) {
            if (v === '') return 'La contraseña es obligatoria.';
            if (!REGEX_CONTRASENNA.test(v))
                return 'De 8 a 15 caracteres, con mayúscula, minúscula y número.';
            return '';
        },
        confirmarContrasenna: function (v) {
            if (v === '') return 'Debe confirmar la contraseña.';
            if (v !== campos.contrasenna.value) return 'La confirmación no coincide.';
            return '';
        }
    };

    // Crea (una sola vez) el div donde se escribe el error de cada campo
    function obtenerContenedorError(input) {
        var existente = input.parentNode.querySelector('.invalid-feedback');
        if (existente) return existente;
        var div = document.createElement('div');
        div.className = 'invalid-feedback';
        input.parentNode.appendChild(div);
        return div;
    }

    function validarCampo(nombre, mostrarError) {
        var input = campos[nombre];
        var mensaje = reglas[nombre](input.value);

        if (mostrarError) {
            var cont = obtenerContenedorError(input);
            cont.textContent = mensaje;
            input.classList.toggle('is-invalid', mensaje !== '');
            input.classList.toggle('is-valid', mensaje === '');
        }
        return mensaje === '';
    }

    function validarFormulario(mostrarError) {
        var ok = true;
        Object.keys(reglas).forEach(function (nombre) {
            if (!validarCampo(nombre, mostrarError)) ok = false;
        });
        return ok;
    }

    function actualizarBoton() {
        boton.disabled = !validarFormulario(false);
    }

    // Los campos ya tocados muestran su error; el resto solo cuenta para el botón
    Object.keys(campos).forEach(function (nombre) {
        var input = campos[nombre];
        var evento = input.tagName === 'SELECT' ? 'change' : 'input';

        input.addEventListener(evento, function () {
            validarCampo(nombre, true);

            // Si cambia la contraseña, revalidar la confirmación
            if (nombre === 'contrasenna' && campos.confirmarContrasenna.value !== '') {
                validarCampo('confirmarContrasenna', true);
            }
            actualizarBoton();
        });

        input.addEventListener('blur', function () {
            validarCampo(nombre, true);
        });
    });

    // Último seguro antes de enviar
    form.addEventListener('submit', function (e) {
        if (!validarFormulario(true)) {
            e.preventDefault();
        }
    });

    actualizarBoton();
})();