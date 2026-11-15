document.addEventListener("DOMContentLoaded", function () {

    const contenedor = document.getElementById("contenedorIngredientes");
    const botonAgregar = document.getElementById("agregarIngrediente");
    const totalElemento = document.getElementById("totalPorcentaje");
    const estadoElemento = document.getElementById("estadoPorcentaje");

    if (!contenedor || !botonAgregar || !totalElemento || !estadoElemento) return;

    function actualizarTotal() {
        let total = 0;

        document.querySelectorAll(".porcentaje-ingrediente").forEach(function (campo) {
            const valor = parseFloat(campo.value);
            if (!isNaN(valor)) {
                total += valor;
            }
        });

        totalElemento.textContent = total.toFixed(2);

        if (Math.abs(total - 100) < 0.001) {
            estadoElemento.textContent = "(Porcentaje correcto)";
            estadoElemento.style.color = "green";
        } else {
            estadoElemento.textContent = "(Debe sumar 100%)";
            estadoElemento.style.color = "red";
        }
    }

    function actualizarOpciones() {
        const filas = contenedor.querySelectorAll(".ingrediente-fila");
        const seleccionados = [];

        filas.forEach(function (fila) {
            const select = fila.querySelector(".selector-ingrediente");
            if (select && select.value !== "") {
                seleccionados.push(select.value);
            }
        });

        filas.forEach(function (fila) {
            const select = fila.querySelector(".selector-ingrediente");
            if (!select) return;

            Array.from(select.options).forEach(function (opcion) {
                if (opcion.value === "") return;

                const estaSeleccionadoEnOtraFila =
                    seleccionados.includes(opcion.value) &&
                    select.value !== opcion.value;

                opcion.disabled = estaSeleccionadoEnOtraFila;
            });
        });
    }

    function conectarFila(fila) {
        const porcentaje = fila.querySelector(".porcentaje-ingrediente");
        const selector = fila.querySelector(".selector-ingrediente");
        const botonQuitar = fila.querySelector(".quitar-ingrediente");

        porcentaje.addEventListener("input", actualizarTotal);

        selector.addEventListener("change", actualizarOpciones);

        botonQuitar.addEventListener("click", function () {
            if (contenedor.querySelectorAll(".ingrediente-fila").length <= 1) {
                alert("Debe conservar al menos una fila de ingrediente.");
                return;
            }

            fila.remove();
            actualizarTotal();
            actualizarOpciones();
        });
    }

    function crearFila() {
        const filaOriginal = contenedor.querySelector(".ingrediente-fila");
        const nuevaFila = filaOriginal.cloneNode(true);

        nuevaFila.querySelector(".selector-ingrediente").value = "";
        nuevaFila.querySelector(".porcentaje-ingrediente").value = "";

        contenedor.appendChild(nuevaFila);

        conectarFila(nuevaFila);
        actualizarTotal();
        actualizarOpciones();
    }

    botonAgregar.addEventListener("click", crearFila);

    contenedor.querySelectorAll(".ingrediente-fila").forEach(conectarFila);

    actualizarTotal();
    actualizarOpciones();
});