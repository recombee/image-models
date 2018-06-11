@echo off
call Common.cmd

DeepRecommender.exe d Host='localhost';Database='recommender';Username='postgres';Password='PASSWD' DATABASE_NAME R:\\RecTest\\interactions

pause