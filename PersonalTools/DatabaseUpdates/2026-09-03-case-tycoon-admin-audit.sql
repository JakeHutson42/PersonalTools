-- Append-only record of successful Case Tycoon administrator mutations.
USE PersonalTools;

CREATE TABLE IF NOT EXISTS CaseTycoonAdminAuditLog
(
    AuditId CHAR(36) NOT NULL,
    ActorUserId CHAR(36) NOT NULL,
    ActorDisplayName VARCHAR(100) NOT NULL,
    HttpMethod VARCHAR(10) NOT NULL,
    RequestPath VARCHAR(500) NOT NULL,
    RouteValues VARCHAR(1000) NOT NULL,
    SubmittedJson MEDIUMTEXT NOT NULL,
    ResponseStatus SMALLINT UNSIGNED NOT NULL,
    RemoteIp VARCHAR(64) NOT NULL,
    UserAgent VARCHAR(512) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(AuditId),
    KEY IX_CaseTycoonAdminAuditLog_CreatedUtc(CreatedUtc),
    KEY IX_CaseTycoonAdminAuditLog_Actor_Created(ActorUserId,CreatedUtc),
    KEY IX_CaseTycoonAdminAuditLog_Path_Created(RequestPath(191),CreatedUtc)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_tycoon_admin_audit_append//
CREATE PROCEDURE sp_case_tycoon_admin_audit_append(
    IN p_audit_id CHAR(36), IN p_actor_user_id CHAR(36), IN p_actor_display_name VARCHAR(100),
    IN p_http_method VARCHAR(10), IN p_request_path VARCHAR(500), IN p_route_values VARCHAR(1000),
    IN p_submitted_json MEDIUMTEXT, IN p_response_status SMALLINT UNSIGNED,
    IN p_remote_ip VARCHAR(64), IN p_user_agent VARCHAR(512), IN p_created_utc DATETIME(6))
BEGIN
    INSERT INTO CaseTycoonAdminAuditLog
        (AuditId,ActorUserId,ActorDisplayName,HttpMethod,RequestPath,RouteValues,SubmittedJson,ResponseStatus,RemoteIp,UserAgent,CreatedUtc)
    VALUES
        (p_audit_id,p_actor_user_id,p_actor_display_name,p_http_method,p_request_path,p_route_values,p_submitted_json,p_response_status,p_remote_ip,p_user_agent,p_created_utc);
END//
DELIMITER ;
