-- Feedback (👍/👎) por respuesta del asistente IA, con motivo opcional y,
-- si el usuario lo autoriza, la pregunta y respuesta completas para poder
-- ajustar el prompt o el manual de ayuda a partir de casos concretos.
-- Aplicar manualmente contra la base de dev (10.6.46.17 / HTEST01) — la app usa
-- hbm2ddl=none, el esquema no se genera automáticamente.

CREATE TABLE USUARIO.ASISTENTE_FEEDBACK (
    ID                   NUMBER(10)     NOT NULL,
    RESPUESTA_ID         VARCHAR2(64)   NOT NULL,
    USUARIO              VARCHAR2(100)  NOT NULL,
    UTIL                 NUMBER(1)      NOT NULL,
    MOTIVO               VARCHAR2(30),
    COMENTARIO           VARCHAR2(2000),
    HERRAMIENTAS         VARCHAR2(500),
    CONSULTA_USUARIO     VARCHAR2(4000),
    RESPUESTA_ASISTENTE  CLOB,
    MODELO               VARCHAR2(100)  NOT NULL,
    FECHA                DATE           NOT NULL,
    CONSTRAINT PK_ASISTENTE_FEEDBACK PRIMARY KEY (ID),
    CONSTRAINT UQ_ASISTENTE_FEEDBACK_RESP UNIQUE (RESPUESTA_ID),
    CONSTRAINT CK_ASISTENTE_FEEDBACK_UTIL CHECK (UTIL IN (0, 1))
);

CREATE SEQUENCE USUARIO.ASISTENTE_FEEDBACK_SEQ
    START WITH 1
    INCREMENT BY 1
    NOCACHE;
