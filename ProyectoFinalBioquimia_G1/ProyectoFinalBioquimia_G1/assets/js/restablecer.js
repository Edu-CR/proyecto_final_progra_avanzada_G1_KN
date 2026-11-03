(function () {
    const btn = document.getElementById('btn-submit');

    const regexCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const regexToken = /^\d{10}$/;
    const regexContrasenna = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,15}$/;

    const correo = document.getElementById('correoElectronico');
    const token = document.getElementById('Token');
    const pass = document.getElementById('Contrasenna');
    const confirmar = document.getElementById('ConfirmarContrasenna');

    // En el orden pedido: vacío -> formato. Devuelve el mensaje de error o '' si está bien.
    const campos = [
        {
            el: correo,
            validar: v => !v ? 'El correo es obligatorio.'
                : !regexCorreo.test(v) ? 'El correo no tiene un formato válido.' : ''
        },
        {
            el: token,
            validar: v => !v ? 'El código es obligatorio.'
                : !regexToken.test(v) ? 'El código debe tener exactamente 10 dígitos.' : ''
        },
        {
            el: pass,
            validar: v => !v ? 'La contraseña es obligatoria.'
                : !regexContrasenna.test(v) ? 'Debe tener de 8 a 15 caracteres, con mayúscula, minúscula y número.' : ''
        },
        {
            el: confirmar,
            validar: v => !v ? 'Debe confirmar la contraseña.'
                : v !== pass.value ? 'Las contraseñas no coinciden.' : ''
        }
    ];

    // Campos que el usuario ya tocó (para no mostrar errores desde el inicio)
    const tocados = new Set();

    function mostrarEstado(el, mensaje) {
        const feedback = document.querySelector('[data-feedback-for="' + el.id + '"]');
        const tocado = tocados.has(el.id);
        const hayError = tocado && mensaje !== '';

        el.classList.toggle('is-invalid', hayError);
        el.classList.toggle('is-valid', tocado && mensaje === '');
        feedback.textContent = hayError ? mensaje : '';
        feedback.classList.toggle('d-block', hayError);
    }

    function validarTodo() {
        let todoValido = true;

        campos.forEach(function (campo) {
            const mensaje = campo.validar(campo.el.value.trim());
            mostrarEstado(campo.el, mensaje);
            if (mensaje !== '') todoValido = false;
        });

        btn.disabled = !todoValido;
    }

    // El código solo acepta dígitos
    token.addEventListener('input', function () {
        token.value = token.value.replace(/\D/g, '');
    });

    [token, pass, confirmar].forEach(function (el) {
        el.addEventListener('input', function () {
            tocados.add(el.id);
            // Si cambia la contraseña y ya hay confirmación, se revisa de nuevo
            if (el === pass && confirmar.value) tocados.add(confirmar.id);
            validarTodo();
        });
    });

    // El correo viene del servidor y es de solo lectura: se marca como tocado desde el inicio
    tocados.add(correo.id);

    // Evita enviar si por alguna razón el formulario no es válido
    document.getElementById('restablecerForm').addEventListener('submit', function (e) {
        validarTodo();
        if (btn.disabled) e.preventDefault();
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

    validarTodo();
})();