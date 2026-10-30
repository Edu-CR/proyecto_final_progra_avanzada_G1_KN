# ⚙️ Configuración de Entity Framework (EF) porque la carpeta EF esta excluida en el .gitignore

La carpeta `EF/` está excluida del repositorio mediante el archivo `.gitignore`. Esto permite que cada desarrollador configure su propia cadena de conexión y su entorno de Base de Datos local de forma independiente, evitando conflictos en Git.

> [!WARNING]
> **Todos debemos usar exactamente los mismos nombres** (modelo, conexión y namespace). Si los nombres no coinciden, el código de los demás no compilará en tu máquina y cambiarlos después es muy difícil.

---

## 📌 Resumen: nombres obligatorios

| Qué | Nombre exacto | Paso |
|---|---|---|
| 📄 Nombre del modelo (item) | `BaseDeDatosModel` | 3 |
| 🔌 Nombre de la conexión | `BioquimiaDBEntities` | 7 |
| 🏷️ Namespace del modelo | `BioquimiaDBModel` | 8 |
| 🚫 Pluralizar tablas | **DESMARCADO** | 8 |

---

## 🚀 Pasos para configurar la conexión local

### 1️⃣ Base de Datos

* Monte la base de datos local utilizando el archivo disponible en la carpeta `BASE DE DATOS`.

### 2️⃣ Generación del modelo EF

* Abra la solución en **Visual Studio**.
* Haga clic derecho en el proyecto `ProyectoFinalBioquimia_G1`.
* Siga los pasos a continuación para que todos tengamos el mismo modelo y los mismos nombres de conexión (así lo hizo el profe en clase).

---

#### Paso 1 — Agregar un nuevo item

Click derecho en la carpeta `EF` → **Add** → **New item...**

<img width="1232" height="452" alt="image" src="https://github.com/user-attachments/assets/db9a2d0f-f80b-4043-ad2d-8be787981eda" />

---

#### Paso 2 — Mostrar todas las plantillas

Si aparece este mensaje, haga click en **Show all templates**.

<img width="622" height="265" alt="image" src="https://github.com/user-attachments/assets/e23d2fd2-a6e8-4fb0-ac95-bfd141fe1183" />

---

#### Paso 3 — Crear el modelo con el nombre correcto

A la izquierda seleccione **Data** → **ADO.NET Entity Data Model** y ponga como nombre del modelo:

```
BaseDeDatosModel
```

> [!IMPORTANT]
> 🔴 **PASO MUY IMPORTANTE:** el nombre debe ser exactamente `BaseDeDatosModel`. Si no, cuesta mucho cambiarlo después.

<img width="1222" height="825" alt="image" src="https://github.com/user-attachments/assets/eb54c032-9315-4c38-b6ae-a48bb0820a08" />

---

#### Paso 4 — Elegir el tipo de modelo

Seleccione **EF Designer from database**.

<img width="682" height="771" alt="image" src="https://github.com/user-attachments/assets/d09574dc-e9b5-4ac3-afd9-4dc5abb448ab" />

---

#### Paso 5 — Nueva conexión

Click en **New Connection**.

<img width="716" height="762" alt="image" src="https://github.com/user-attachments/assets/975ce91b-fc38-45e6-9e3d-9ab9d901d21f" />

---

#### Paso 6 — Configurar el servidor

* Use `localhost` (así lo hacemos en clase).
* Marque **Trust server certificate**.
* Escriba el nombre de la base de datos.
* Click en **Test Connection** para verificar que funcione.

<img width="642" height="926" alt="image" src="https://github.com/user-attachments/assets/9bf6d062-482c-4480-b674-ac70e4237bcf" />
<img width="663" height="947" alt="image" src="https://github.com/user-attachments/assets/f87b3d0a-935c-4950-a6cc-651b61137f55" />

---

#### Paso 7 — Nombre de la conexión

Guarde el nombre de la conexión como:

```
BioquimiaDBEntities
```

> [!IMPORTANT]
> 🔴 **PASO MUY IMPORTANTE:** el nombre debe ser exactamente `BioquimiaDBEntities`. Si no, cuesta mucho cambiarlo después.

<img width="748" height="802" alt="image" src="https://github.com/user-attachments/assets/8081a5af-28df-47cd-8523-4e2eab4fef0e" />

---

#### Pasos 8 y 9 — Seleccionar tablas y opciones

* Seleccione **todas las tablas**.
* Verifique que el **Model Namespace** sea `BioquimiaDBModel`.
* **NO** marque **Pluralize or singularize generated object names**.
* Verifique que esté **marcada** la casilla **Include foreign key columns in the model**.
* Click en **Finish**.

> [!CAUTION]
> 🚫 **PASO MUY IMPORTANTE: ¡NUNCA PLURALIZAR TABLAS!**
> El namespace debe ser `BioquimiaDBModel` y la casilla de pluralizar debe quedar desmarcada. Si no, cuesta mucho cambiarlo después y en este proyecto **no trabajamos con tablas pluralizadas**.

<img width="747" height="790" alt="image" src="https://github.com/user-attachments/assets/41c7a39d-683c-4a5d-9c93-19edd1d59cb7" />

---

#### Paso 10 — Mensajes de advertencia

Haga click en **OK** en los mensajes de warning.

<img width="742" height="287" alt="image" src="https://github.com/user-attachments/assets/1e1021d2-56df-45f5-ae82-f840456d2113" />
<img width="636" height="265" alt="image" src="https://github.com/user-attachments/assets/0725b078-b9a4-4c17-9aef-97dc79519db8" />

---

#### Paso 11 — ¡Listo! ✅

Al finalizar, el modelo debería visualizarse de esta forma:

<img width="2560" height="1310" alt="image" src="https://github.com/user-attachments/assets/76e77b73-a907-4d45-96fd-99b665740605" />

---

## ✅ Checklist final

- [ ] Base de datos montada localmente
- [ ] Modelo llamado `BaseDeDatosModel`
- [ ] Conexión llamada `BioquimiaDBEntities`
- [ ] Namespace `BioquimiaDBModel`
- [ ] Pluralizar tablas **desmarcado**
- [ ] *Include foreign key columns* **marcado**
