

CREATE DATABASE "WebCrawlerDB"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Portuguese_Brazil.1252'
    LC_CTYPE = 'Portuguese_Brazil.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;


CREATE TABLE public.tb_web_crawler_infos
(
    id integer NOT NULL,
    dt_inicio_exec timestamp without time zone,
    dt_termino_exec timestamp without time zone,
    qtd_paginas integer,
    qtd_linhas integer,
    CONSTRAINT tb_web_crawler_infos_pkey PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE public.tb_web_crawler_infos
    OWNER to postgres;