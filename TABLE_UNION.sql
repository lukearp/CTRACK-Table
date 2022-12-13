SELECT [ATY_BAR_ID] as BAR_ID
      ,[ATY_FULL_NAME] as FULL_NAME
      ,[ATY_ADDR_1] as ADDR_1
      ,[ATY_ADDR_2] as ADDR_2
      ,[ATY_CITY] as CITY
      ,[ATY_STATE] as US_STATE
      ,[ATY_ZIP] as ZIP
      ,[ATY_EMAIL] as EMAIL
      ,[ATY_PHONE] as PHONE
      ,[ATY_FAX] as FAX
      ,[ATY_DOA] as ADMIT_DATE
      ,[ATY_STATUS] as ACTOR_STATUS
      ,TYPEID = '10000'
  FROM [dbo].[ATTMSTR]

UNION ALL

  SELECT  [JUD_BAR_ID] as BAR_ID
        ,[JUD_NAME] as FULL_NAME
        ,[JUD_MAIL_ADD1] as ADDR_1
        ,[JUD_MAIL_ADD2] as ADDR_2
        ,[JUD_MAIL_CITY] as CITY
        ,[JUD_MAIL_STATE] as US_STATE
        ,[JUD_MAIL_ZIP_5] as ZIP
        ,[JUD_EMAIL] as EMAIL
        ,[JUD_PHONE] as PHONE
        ,[JUD_PHONE] as FAX
        ,[AddDate] as ADMIT_DATE
        ,[JUD_ACTIVE] as ACTOR_STATUS 
        ,TYPEID = '10001'     
  FROM [dbo].[JUDMAST]