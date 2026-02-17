using UnityEngine;

namespace Utils
{
    public static class FileNameUtils
    {
        /// <summary>
        /// 파일명에서 모든 공백을 제거합니다.
        /// </summary>
        /// <param name="originalName">원본 파일명</param>
        /// <returns>공백이 제거된 파일명</returns>
        public static string RemoveSpaces(string originalName)
        {
            if (string.IsNullOrEmpty(originalName))
                return originalName;
                
            return originalName.Replace(" ", "");
        }
        
        /// <summary>
        /// 번역 테이블에서 사용할 키를 생성합니다. 공백을 제거한 형태로 반환합니다.
        /// </summary>
        /// <param name="originalKey">원본 텍스트</param>
        /// <returns>공백이 제거된 키</returns>
        public static string GetTranslationKey(string originalKey)
        {
            return RemoveSpaces(originalKey);
        }
        
        /// <summary>
        /// 파일 시스템에서 사용할 수 없는 문자를 제거합니다.
        /// </summary>
        /// <param name="name">정리할 파일명</param>
        /// <returns>정리된 파일명</returns>
        public static string SanitizeForFileSystem(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
                
            // Windows 파일 시스템에서 사용할 수 없는 문자 제거
            char[] invalidChars = { '<', '>', ':', '"', '|', '?', '*' };
            string result = name;
            
            foreach (char invalidChar in invalidChars)
            {
                result = result.Replace(invalidChar.ToString(), "");
            }
            
            return result;
        }
    }
}
