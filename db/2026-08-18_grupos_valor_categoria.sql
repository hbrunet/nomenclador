-- Grupos de tipos de valor por categoría (agrupan USUARIO.DESC_VALCAT), mismo
-- patrón que USUARIO.GRUPOVALFIJO (db/2026-08-14_grupos_valor_fijo.sql) pero para
-- los tipos de valor por categoría, que viven en una tabla catálogo distinta.
-- Aplicar manualmente contra la base de dev (10.6.46.17 / HTEST01) — la app usa
-- hbm2ddl=none, el esquema no se genera automáticamente.

CREATE TABLE USUARIO.GRUPOVALCAT (
    ID          NUMBER(10)    NOT NULL,
    DESCRIPCION VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_GRUPOVALCAT PRIMARY KEY (ID)
);

CREATE SEQUENCE USUARIO.GRUPOVALCAT_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE;

CREATE TABLE USUARIO.GRUPOVALCAT_TIPO (
    IDGRUPO NUMBER(10) NOT NULL,
    IDDVCAT NUMBER(10) NOT NULL,
    CONSTRAINT PK_GRUPOVALCAT_TIPO PRIMARY KEY (IDGRUPO, IDDVCAT),
    CONSTRAINT FK_GRPVALCATTIPO_GRUPO FOREIGN KEY (IDGRUPO)
        REFERENCES USUARIO.GRUPOVALCAT (ID) ON DELETE CASCADE,
    CONSTRAINT FK_GRPVALCATTIPO_TIPO FOREIGN KEY (IDDVCAT)
        REFERENCES USUARIO.DESC_VALCAT (IDDVCAT)
);

-- Grupo inicial "Mensual": tipos que se asocian todos los meses.
DECLARE
    v_id_grupo USUARIO.GRUPOVALCAT.ID%TYPE;
BEGIN
    v_id_grupo := USUARIO.GRUPOVALCAT_SEQ.NEXTVAL;

    INSERT INTO USUARIO.GRUPOVALCAT (ID, DESCRIPCION)
    VALUES (v_id_grupo, 'Mensual');

    FOR v_id_tipo IN (
        SELECT COLUMN_VALUE AS ID
        FROM TABLE(SYS.ODCINUMBERLIST(
            1, 23, 24, 33, 35, 41, 54, 71, 72, 73, 77, 78, 79, 80, 86
        ))
    ) LOOP
        INSERT INTO USUARIO.GRUPOVALCAT_TIPO (IDGRUPO, IDDVCAT)
        VALUES (v_id_grupo, v_id_tipo.ID);
    END LOOP;

    COMMIT;
END;
/

-- Grupo inicial "Mensual Policías": tipos que se asocian todos los meses para policías.
DECLARE
    v_id_grupo USUARIO.GRUPOVALCAT.ID%TYPE;
BEGIN
    v_id_grupo := USUARIO.GRUPOVALCAT_SEQ.NEXTVAL;

    INSERT INTO USUARIO.GRUPOVALCAT (ID, DESCRIPCION)
    VALUES (v_id_grupo, 'Mensual Policías');

    FOR v_id_tipo IN (
        SELECT COLUMN_VALUE AS ID
        FROM TABLE(SYS.ODCINUMBERLIST(
            45, 46, 10, 43, 33, 1, 90, 91
        ))
    ) LOOP
        INSERT INTO USUARIO.GRUPOVALCAT_TIPO (IDGRUPO, IDDVCAT)
        VALUES (v_id_grupo, v_id_tipo.ID);
    END LOOP;

    COMMIT;
END;
/


