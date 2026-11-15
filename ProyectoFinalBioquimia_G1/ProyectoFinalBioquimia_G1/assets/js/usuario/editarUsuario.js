
(function () {
    'use strict';

    var REGEX_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    var REGEX_ID = /^[0-9]{8,20}$/;

    var campos = {
        identificacion: document.getElementById('identificacion'),
        nombreCompleto: document.getElementById('nombreCompleto'),
        correoElectronico: document.getElementById('correoElectronico'),
        idRol: document.getElementById('idRol')
    };

    var form = campos.identificacion.closest('form');
    var boton = form.querySelector('button[type="submit"]');

    var reglas = {
        identificacion: function (v) {
            v = v.trim();
            if (v === '') return 'La identificación es obligatoria.';
            if (!REGEX_ID.test(v)) return 'La identificación debe tener entre 8 y 20 dígitos.';
            return '';
        },
        nombreCompleto: function (v) {
            return v.trim() === '' ? 'El nombre completo es obligatorio.' : '';
        },
        correoElectronico: function (v) {
            v = v.trim();
            if (v === '') return 'El correo es obligatorio.';
            if (!REGEX_CORREO.test(v)) return 'El correo no tiene un formato válido.';
            return '';
        },
        idRol: function (v) {
            return v === '' ? 'Seleccione un rol.' : '';
        }
    };

    // Si el rol está bloqueado, no se valida porque no se puede modificar.
    if (!campos.idRol || campos.idRol.disabled) {
        delete campos.idRol;
        delete reglas.idRol;
    }

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
            obtenerContenedorError(input).textContent = mensaje;
            input.classList.toggle('is-invalid', mensaje !== '');
            input.classList.toggle('is-valid', mensaje === '');
        }

        return mensaje === '';
    }

    function validarFormulario(mostrarError) {
        var valido = true;

        Object.keys(reglas).forEach(function (nombre) {
            if (!validarCampo(nombre, mostrarError)) {
                valido = false;
            }
        });

        return valido;
    }

    function actualizarBoton() {
        boton.disabled = !validarFormulario(false);
    }

    Object.keys(campos).forEach(function (nombre) {
        var input = campos[nombre];

        if (!input) return;

        var evento = input.tagName === 'SELECT' ? 'change' : 'input';

        input.addEventListener(evento, function () {
            validarCampo(nombre, true);
            actualizarBoton();
        });

        input.addEventListener('blur', function () {
            validarCampo(nombre, true);
        });
    });

    form.addEventListener('submit', function (e) {
        if (!validarFormulario(true)) {
            e.preventDefault();
        }
    });

    // Validar los datos existentes al cargar la vista.
    actualizarBoton();

})();
