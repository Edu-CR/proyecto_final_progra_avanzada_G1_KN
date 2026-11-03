(function () {
    const btn = document.getElementById('btn-submit');
    const form = document.getElementById('loginForm');

    const correo = document.getElementById('CorreoElectronico');
    const pass = document.getElementById('Contrasenna');

    const regexCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    // Vacío -> formato. Devuelve el mensaje de error o '' si está bien.
    const campos = [
        {
            el: correo,
            validar: v => !v ? 'El correo es obligatorio.'
                : !regexCorreo.test(v) ? 'El correo no tiene un formato válido.' : ''
        },
        {
            el: pass,
         
            validar: v => !v ? 'La contraseña es obligatoria.' : ''
        }
    ];

    // Campos que el usuario ya tocó (para no mostrar errores desde el inicio)
    const tocados = new Set();

    function mostrarEstado(el, mensaje) {
        const feedback = document.querySelector('[data-feedback-for="' + el.id + '"]');
        const tocado = tocados.has(el.id);
        const hayError = tocado && mensaje !== '';

        el.classList.toggle('is-invalid', hayError);
        feedback.textContent = hayError ? mensaje : '';
        feedback.classList.toggle('d-block', hayError);
    }

    function validarTodo() {
        let todoValido = true;

        campos.forEach(function (campo) {
            const valor = campo.el === pass ? campo.el.value : campo.el.value.trim();
            const mensaje = campo.validar(valor);
            mostrarEstado(campo.el, mensaje);
            if (mensaje !== '') todoValido = false;
        });

        btn.disabled = !todoValido;
    }

    [correo, pass].forEach(function (el) {
        el.addEventListener('input', function () {
            tocados.add(el.id);
            validarTodo();
        });
    });

    // Ojito para mostrar/ocultar contraseña
    document.querySelectorAll('.btn-toggle-password').forEach(function (boton) {
        boton.addEventListener('click', function () {
            const input = document.getElementById(boton.dataset.target);
            const icono = boton.querySelector('i');
            const mostrar = input.type === 'password';

            input.type = mostrar ? 'text' : 'password';
            icono.classList.toggle('bi-eye', !mostrar);
            icono.classList.toggle('bi-eye-slash', mostrar);
            boton.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
        });
    });

    // Evita enviar si el formulario no es válido
    form.addEventListener('submit', function (e) {
        validarTodo();
        if (btn.disabled) e.preventDefault();
    });

    // Si el navegador autocompletó los campos, se marcan como tocados y se valida
    if (correo.value || pass.value) {
        if (correo.value) tocados.add(correo.id);
        if (pass.value) tocados.add(pass.id);
    }

    validarTodo();
})();