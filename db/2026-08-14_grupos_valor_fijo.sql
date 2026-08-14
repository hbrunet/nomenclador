-- Grupos de tipos de valor fijo (agrupan USUARIO.DESC_VALFIJO) para acelerar la
-- clonación masiva mensual de valores fijos: en vez de re-seleccionar los mismos
-- tipos cada mes, se elige un grupo guardado.
-- Aplicar manualmente contra la base de dev (10.6.46.17 / HTEST01) — la app usa
-- hbm2ddl=none, el esquema no se genera automáticamente.

CREATE TABLE USUARIO.GRUPOVALFIJO (
    ID          NUMBER(10)    NOT NULL,
    DESCRIPCION VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_GRUPOVALFIJO PRIMARY KEY (ID)
);

CREATE SEQUENCE USUARIO.GRUPOVALFIJO_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE;

CREATE TABLE USUARIO.GRUPOVALFIJO_TIPO (
    IDGRUPO NUMBER(10) NOT NULL,
    IDDVF   NUMBER(10) NOT NULL,
    CONSTRAINT PK_GRUPOVALFIJO_TIPO PRIMARY KEY (IDGRUPO, IDDVF),
    CONSTRAINT FK_GRPVALFIJOTIPO_GRUPO FOREIGN KEY (IDGRUPO)
        REFERENCES USUARIO.GRUPOVALFIJO (ID) ON DELETE CASCADE,
    CONSTRAINT FK_GRPVALFIJOTIPO_TIPO FOREIGN KEY (IDDVF)
        REFERENCES USUARIO.DESC_VALFIJO (IDDVF)
);

-- Grupo inicial "Mensual": tipos que se clonan todos los meses.
DECLARE
    v_id_grupo USUARIO.GRUPOVALFIJO.ID%TYPE;
BEGIN
    v_id_grupo := USUARIO.GRUPOVALFIJO_SEQ.NEXTVAL;

    INSERT INTO USUARIO.GRUPOVALFIJO (ID, DESCRIPCION)
    VALUES (v_id_grupo, 'Mensual');

    FOR v_id_tipo IN (
        SELECT COLUMN_VALUE AS ID
        FROM TABLE(SYS.ODCINUMBERLIST(
            16, 17, 18, 145, 156, 161, 182, 184, 191, 193, 194, 215, 217, 220,
            233, 235, 241, 242, 246, 257, 258, 263, 267, 271, 272, 276, 281,
            226, 284
        ))
    ) LOOP
        INSERT INTO USUARIO.GRUPOVALFIJO_TIPO (IDGRUPO, IDDVF)
        VALUES (v_id_grupo, v_id_tipo.ID);
    END LOOP;

    COMMIT;
END;
/
